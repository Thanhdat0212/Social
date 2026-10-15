using System.Text.Json;
using Application.Common;
using Application.DTOs.Marketplace.Shops;
using Application.Interfaces;
using Application.Interfaces.Marketplace.Shops;
using Application.Interfaces.Repositories;
using Domain.Entities.Marketplace.Shops;
using Domain.Enums;
using Domain.Exceptions;
using Infrastructure.External.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Shops;

public class ShopService : IShopService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notificationService;
    private readonly ICacheService _cacheService;
    private readonly IClock _clock;
    private readonly ILogger<ShopService> _logger;

    public ShopService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        INotificationService notificationService,
        ICacheService cacheService,
        IClock clock,
        ILogger<ShopService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
        _cacheService = cacheService;
        _clock = clock;
        _logger = logger;
    }

    public async Task<ShopDetailDto> CreateShopAsync(CreateShopRequestDto request, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        if (user == null) throw new NotFoundException("User", currentUserId);

        if (!user.EmailConfirmed)
        {
            throw new ValidationAppException("Email", "Bạn cần xác thực email trước khi đăng ký mở gian hàng.");
        }

        var hasShop = await _unitOfWork.Shops.ExistsByOwnerUserIdAsync(currentUserId, cancellationToken);
        if (hasShop)
        {
            throw new ValidationAppException("OwnerUserId", "Mỗi tài khoản chỉ được phép sở hữu tối đa một gian hàng.");
        }

        var slug = SlugHelper.GenerateSlug(string.IsNullOrWhiteSpace(request.Slug) ? request.Name : request.Slug);
        var slugExists = await _unitOfWork.Shops.ExistsBySlugAsync(slug, cancellationToken);
        if (slugExists)
        {
            throw new ValidationAppException("Slug", $"Tên định danh (slug) '{slug}' đã được sử dụng. Vui lòng chọn tên khác.");
        }

        var shop = new Shop(
            ownerUserId: currentUserId,
            name: request.Name,
            slug: slug,
            contactEmail: request.ContactEmail,
            description: request.Description,
            logoUrl: request.LogoUrl);

        var snapshotJson = JsonSerializer.Serialize(new
        {
            shop.Name,
            shop.Slug,
            shop.ContactEmail,
            shop.Description,
            shop.LogoUrl,
            OwnerUserId = user.Id,
            OwnerDisplayName = user.DisplayName,
            OwnerEmail = user.Email,
            SubmittedAtUtc = _clock.UtcNow
        });

        var review = new ShopReview(shop.Id, submissionNo: 1, snapshotJson: snapshotJson);

        try
        {
            await _unitOfWork.Shops.AddAsync(shop, cancellationToken);
            await _unitOfWork.ShopReviews.AddAsync(review, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi lưu gian hàng vào database (có thể do race condition duplicate owner/slug).");
            throw new ValidationAppException("Shop", "Tên định danh hoặc tài khoản của bạn đã được liên kết với một gian hàng khác.");
        }

        _logger.LogInformation("Người dùng {UserId} vừa gửi yêu cầu mở gian hàng {ShopId} ({Slug})", currentUserId, shop.Id, shop.Slug);

        return MapToDetailDto(shop, user, review, activeSanction: null);
    }

    public async Task<ShopDetailDto?> GetMyShopAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(currentUserId, cancellationToken);
        if (shop == null) return null;

        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        var latestReview = await _unitOfWork.ShopReviews.GetLatestByShopIdAsync(shop.Id, cancellationToken);
        var activeSanction = await _unitOfWork.ShopSanctions.GetActiveSanctionByShopIdAsync(shop.Id, _clock.UtcNow, cancellationToken);

        return MapToDetailDto(shop, user!, latestReview, activeSanction);
    }

    public async Task<ShopDetailDto> UpdateMyShopAsync(UpdateShopRequestDto request, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(currentUserId, cancellationToken);
        if (shop == null) throw new NotFoundException("Bạn chưa có gian hàng.");

        shop.UpdateDetails(request.Name, request.Description, request.LogoUrl, request.ContactEmail);
        _unitOfWork.Shops.Update(shop);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove(CacheKeys.ShopPublic(shop.Slug));

        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        var latestReview = await _unitOfWork.ShopReviews.GetLatestByShopIdAsync(shop.Id, cancellationToken);
        var activeSanction = await _unitOfWork.ShopSanctions.GetActiveSanctionByShopIdAsync(shop.Id, _clock.UtcNow, cancellationToken);

        return MapToDetailDto(shop, user!, latestReview, activeSanction);
    }

    public async Task<ShopDetailDto> ResubmitShopAsync(ResubmitShopRequestDto request, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(currentUserId, cancellationToken);
        if (shop == null) throw new NotFoundException("Bạn chưa có gian hàng.");

        if (shop.ApprovalStatus != ApprovalStatus.Rejected)
        {
            throw new ValidationAppException("Shop", "Chỉ có thể nộp lại hồ sơ khi hồ sơ trước đó bị từ chối.");
        }

        var latestReview = await _unitOfWork.ShopReviews.GetLatestByShopIdAsync(shop.Id, cancellationToken);
        if (latestReview != null && !latestReview.CanResubmit)
        {
            throw new ValidationAppException("Shop", "Hồ sơ của bạn đã bị từ chối vĩnh viễn và không được phép nộp lại.");
        }

        var submissionCount = await _unitOfWork.ShopReviews.GetSubmissionCountAsync(shop.Id, cancellationToken);
        if (submissionCount >= 3)
        {
            throw new ValidationAppException("Shop", "Bạn đã vượt quá số lần nộp lại tối đa (3 lần).");
        }

        if (latestReview?.ReviewedAtUtc != null)
        {
            var cooldownHours = 24;
            var elapsed = _clock.UtcNow - latestReview.ReviewedAtUtc.Value;
            if (elapsed < TimeSpan.FromHours(cooldownHours))
            {
                var remaining = TimeSpan.FromHours(cooldownHours) - elapsed;
                throw new ValidationAppException("Shop", $"Vui lòng đợi thêm {remaining.Hours} giờ {remaining.Minutes} phút trước khi gửi lại hồ sơ.");
            }
        }

        shop.Resubmit(request.Name, request.Description, request.LogoUrl, request.ContactEmail);

        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        var snapshotJson = JsonSerializer.Serialize(new
        {
            shop.Name,
            shop.Slug,
            shop.ContactEmail,
            shop.Description,
            shop.LogoUrl,
            OwnerUserId = user!.Id,
            OwnerDisplayName = user.DisplayName,
            OwnerEmail = user.Email,
            ResubmittedAtUtc = _clock.UtcNow,
            PreviousSubmissionNo = latestReview?.SubmissionNo ?? 0
        });

        var newReview = new ShopReview(shop.Id, submissionNo: submissionCount + 1, snapshotJson: snapshotJson);

        _unitOfWork.Shops.Update(shop);
        await _unitOfWork.ShopReviews.AddAsync(newReview, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove(CacheKeys.ShopPublic(shop.Slug));

        return MapToDetailDto(shop, user, newReview, activeSanction: null);
    }

    public async Task<ShopDetailDto> OpenMyShopAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(currentUserId, cancellationToken);
        if (shop == null) throw new NotFoundException("Bạn chưa có gian hàng.");

        shop.Open();
        _unitOfWork.Shops.Update(shop);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove(CacheKeys.ShopPublic(shop.Slug));

        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        var latestReview = await _unitOfWork.ShopReviews.GetLatestByShopIdAsync(shop.Id, cancellationToken);
        var activeSanction = await _unitOfWork.ShopSanctions.GetActiveSanctionByShopIdAsync(shop.Id, _clock.UtcNow, cancellationToken);

        return MapToDetailDto(shop, user!, latestReview, activeSanction);
    }

    public async Task<ShopDetailDto> CloseMyShopAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(currentUserId, cancellationToken);
        if (shop == null) throw new NotFoundException("Bạn chưa có gian hàng.");

        shop.Close();
        _unitOfWork.Shops.Update(shop);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove(CacheKeys.ShopPublic(shop.Slug));

        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        var latestReview = await _unitOfWork.ShopReviews.GetLatestByShopIdAsync(shop.Id, cancellationToken);
        var activeSanction = await _unitOfWork.ShopSanctions.GetActiveSanctionByShopIdAsync(shop.Id, _clock.UtcNow, cancellationToken);

        return MapToDetailDto(shop, user!, latestReview, activeSanction);
    }

    public async Task<ShopSummaryDto> GetPublicShopBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.ShopPublic(slug);
        var cached = await _cacheService.GetAsync<ShopSummaryDto>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var shop = await _unitOfWork.Shops.GetBySlugAsync(slug, cancellationToken);
        if (shop == null) throw new NotFoundException("Gian hàng không tồn tại.", slug);

        var activeSanction = await _unitOfWork.ShopSanctions.GetActiveSanctionByShopIdAsync(shop.Id, _clock.UtcNow, cancellationToken);
        var effectiveStatus = ShopStatusCalculator.Compute(shop.OwnerUser!, shop, activeSanction != null);

        if (effectiveStatus != ShopEffectiveStatus.Active)
        {
            throw new ValidationAppException("Shop", "Gian hàng này hiện tại không hoạt động hoặc đang tạm khóa.");
        }

        var dto = new ShopSummaryDto
        {
            Id = shop.Id,
            OwnerUserId = shop.OwnerUserId,
            Name = shop.Name,
            Slug = shop.Slug,
            LogoUrl = shop.LogoUrl,
            ContactEmail = shop.ContactEmail,
            ApprovalStatus = shop.ApprovalStatus.ToString(),
            OperatingStatus = shop.OperatingStatus.ToString(),
            EffectiveStatus = effectiveStatus.ToString(),
            CreatedAtUtc = shop.CreatedAtUtc
        };

        _cacheService.Set(cacheKey, dto, TimeSpan.FromMinutes(15));
        return dto;
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Yêu cầu đăng nhập.");
        }
        return _currentUserService.UserId.Value;
    }

    private static ShopDetailDto MapToDetailDto(
        Shop shop,
        Domain.Entities.User user,
        ShopReview? latestReview,
        ShopSanction? activeSanction)
    {
        var hasActiveSanction = activeSanction != null;
        var effectiveStatus = ShopStatusCalculator.Compute(user, shop, hasActiveSanction);

        string? reason = null;
        if (effectiveStatus == ShopEffectiveStatus.Rejected) reason = latestReview?.ReasonNote;
        else if (effectiveStatus == ShopEffectiveStatus.Suspended) reason = activeSanction?.Reason;
        else if (effectiveStatus == ShopEffectiveStatus.UserUnavailable) reason = "Tài khoản người dùng đang bị tạm khóa.";

        return new ShopDetailDto
        {
            Id = shop.Id,
            OwnerUserId = shop.OwnerUserId,
            OwnerDisplayName = user.DisplayName,
            OwnerEmail = user.Email,
            Name = shop.Name,
            Slug = shop.Slug,
            LogoUrl = shop.LogoUrl,
            Description = shop.Description,
            ContactEmail = shop.ContactEmail,
            ApprovalStatus = shop.ApprovalStatus.ToString(),
            OperatingStatus = shop.OperatingStatus.ToString(),
            EffectiveStatus = effectiveStatus.ToString(),
            EffectiveStatusReason = reason,
            CanResubmit = latestReview?.CanResubmit ?? true,
            CreatedAtUtc = shop.CreatedAtUtc,
            UpdatedAtUtc = shop.UpdatedAtUtc,
            LatestReview = latestReview == null ? null : new ShopReviewDto
            {
                Id = latestReview.Id,
                SubmissionNo = latestReview.SubmissionNo,
                Decision = latestReview.Decision.ToString(),
                ReasonCode = latestReview.ReasonCode,
                ReasonNote = latestReview.ReasonNote,
                CanResubmit = latestReview.CanResubmit,
                ReviewedAtUtc = latestReview.ReviewedAtUtc,
                SnapshotJson = latestReview.SnapshotJson
            },
            ActiveSanction = activeSanction == null ? null : new ShopSanctionDto
            {
                Id = activeSanction.Id,
                Type = activeSanction.Type.ToString(),
                Reason = activeSanction.Reason,
                StartAtUtc = activeSanction.StartAtUtc,
                EndAtUtc = activeSanction.EndAtUtc,
                IsActive = true,
                LiftedAtUtc = activeSanction.LiftedAtUtc
            }
        };
    }
}

public static class SlugHelper
{
    public static string GenerateSlug(string phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase)) return Guid.NewGuid().ToString("N")[..8];

        var str = RemoveDiacritics(phrase).ToLowerInvariant().Trim();
        str = System.Text.RegularExpressions.Regex.Replace(str, @"[^a-z0-9\s-]", "");
        str = System.Text.RegularExpressions.Regex.Replace(str, @"\s+", "-");
        str = System.Text.RegularExpressions.Regex.Replace(str, @"-+", "-").Trim('-');

        return string.IsNullOrWhiteSpace(str) ? Guid.NewGuid().ToString("N")[..8] : str;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
        var stringBuilder = new System.Text.StringBuilder(capacity: normalizedString.Length);

        foreach (var c in normalizedString)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder
            .ToString()
            .Normalize(System.Text.NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'D');
    }
}
