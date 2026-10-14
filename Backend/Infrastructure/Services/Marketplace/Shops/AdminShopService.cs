using System.Text.Json;
using Application.Common;
using Application.DTOs.Marketplace.Shops;
using Application.Interfaces;
using Application.Interfaces.Marketplace.Shops;
using Application.Interfaces.Repositories;
using Domain.Entities.Marketplace.Shops;
using Domain.Enums;
using Domain.Exceptions;
using Infrastructure.Caching;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Marketplace.Shops;

public class AdminShopService : IAdminShopService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notificationService;
    private readonly ICacheService _cacheService;
    private readonly IClock _clock;
    private readonly ILogger<AdminShopService> _logger;

    public AdminShopService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        INotificationService notificationService,
        ICacheService cacheService,
        IClock clock,
        ILogger<AdminShopService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
        _cacheService = cacheService;
        _clock = clock;
        _logger = logger;
    }

    public async Task<(IReadOnlyList<ShopSummaryDto> Items, int Total)> GetShopsAsync(
        ApprovalStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var shops = await _unitOfWork.Shops.GetShopsAsync(status, search, page, pageSize, cancellationToken);
        var total = await _unitOfWork.Shops.GetTotalCountAsync(status, search, cancellationToken);

        var items = shops.Select(s =>
        {
            var hasActiveSanction = s.Sanctions.Any(sn => sn.IsActive(_clock.UtcNow));
            var effectiveStatus = ShopStatusCalculator.Compute(s.OwnerUser!, s, hasActiveSanction);

            return new ShopSummaryDto
            {
                Id = s.Id,
                OwnerUserId = s.OwnerUserId,
                Name = s.Name,
                Slug = s.Slug,
                LogoUrl = s.LogoUrl,
                ContactEmail = s.ContactEmail,
                ApprovalStatus = s.ApprovalStatus.ToString(),
                OperatingStatus = s.OperatingStatus.ToString(),
                EffectiveStatus = effectiveStatus.ToString(),
                CreatedAtUtc = s.CreatedAtUtc
            };
        }).ToList();

        return (items, total);
    }

    public async Task<ShopDetailDto> GetShopDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var shop = await _unitOfWork.Shops.GetByIdAsync(id, cancellationToken);
        if (shop == null) throw new NotFoundException("Shop", id);

        var latestReview = await _unitOfWork.ShopReviews.GetLatestByShopIdAsync(shop.Id, cancellationToken);
        var activeSanction = await _unitOfWork.ShopSanctions.GetActiveSanctionByShopIdAsync(shop.Id, _clock.UtcNow, cancellationToken);
        var allReviews = await _unitOfWork.ShopReviews.GetByShopIdAsync(shop.Id, cancellationToken);
        var allSanctions = await _unitOfWork.ShopSanctions.GetByShopIdAsync(shop.Id, cancellationToken);

        var dto = MapToDetailDto(shop, shop.OwnerUser!, latestReview, activeSanction);
        dto.Reviews = allReviews.Select(r => new ShopReviewDto
        {
            Id = r.Id,
            SubmissionNo = r.SubmissionNo,
            Decision = r.Decision.ToString(),
            ReasonCode = r.ReasonCode,
            ReasonNote = r.ReasonNote,
            CanResubmit = r.CanResubmit,
            ReviewedAtUtc = r.ReviewedAtUtc,
            SnapshotJson = r.SnapshotJson
        }).ToList();

        dto.Sanctions = allSanctions.Select(sn => new ShopSanctionDto
        {
            Id = sn.Id,
            Type = sn.Type.ToString(),
            Reason = sn.Reason,
            StartAtUtc = sn.StartAtUtc,
            EndAtUtc = sn.EndAtUtc,
            IsActive = sn.IsActive(_clock.UtcNow),
            LiftedAtUtc = sn.LiftedAtUtc
        }).ToList();

        return dto;
    }

    public async Task<ShopDetailDto> ApproveShopAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var adminId = GetCurrentAdminId();
        var shop = await _unitOfWork.Shops.GetByIdAsync(id, cancellationToken);
        if (shop == null) throw new NotFoundException("Shop", id);

        var latestReview = await _unitOfWork.ShopReviews.GetLatestByShopIdAsync(shop.Id, cancellationToken);
        if (latestReview == null || latestReview.Decision != ShopReviewDecision.Pending)
        {
            throw new ValidationAppException("Review", "Không tìm thấy hồ sơ đang ở trạng thái chờ duyệt (Pending) cho gian hàng này.");
        }

        shop.Approve();
        latestReview.Approve(adminId);

        _unitOfWork.Shops.Update(shop);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove(CacheKeys.ShopPublic(shop.Slug));

        await _notificationService.SendNotificationAsync(new Application.DTOs.Notifications.CreateNotificationDto
        {
            UserId = shop.OwnerUserId,
            Type = "shop_approved",
            Title = "Gian hàng của bạn đã được phê duyệt!",
            Body = $"Chúc mừng! Gian hàng '{shop.Name}' đã được Admin phê duyệt. Bạn đã có thể bắt đầu đăng bán sản phẩm.",
            DataJson = JsonSerializer.Serialize(new { ShopId = shop.Id, shop.Slug })
        }, cancellationToken);

        _logger.LogInformation("Admin {AdminId} đã phê duyệt gian hàng {ShopId} ({Slug})", adminId, shop.Id, shop.Slug);

        return await GetShopDetailAsync(id, cancellationToken);
    }

    public async Task<ShopDetailDto> RejectShopAsync(Guid id, RejectShopRequestDto request, CancellationToken cancellationToken = default)
    {
        var adminId = GetCurrentAdminId();

        if (string.IsNullOrWhiteSpace(request.ReasonNote))
        {
            throw new ValidationAppException("ReasonNote", "Từ chối duyệt gian hàng bắt buộc phải có lý do cụ thể.");
        }

        var shop = await _unitOfWork.Shops.GetByIdAsync(id, cancellationToken);
        if (shop == null) throw new NotFoundException("Shop", id);

        var latestReview = await _unitOfWork.ShopReviews.GetLatestByShopIdAsync(shop.Id, cancellationToken);
        if (latestReview == null || latestReview.Decision != ShopReviewDecision.Pending)
        {
            throw new ValidationAppException("Review", "Không tìm thấy hồ sơ đang ở trạng thái chờ duyệt (Pending) cho gian hàng này.");
        }

        shop.Reject();
        latestReview.Reject(adminId, request.ReasonCode, request.ReasonNote, request.CanResubmit);

        _unitOfWork.Shops.Update(shop);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove(CacheKeys.ShopPublic(shop.Slug));

        await _notificationService.SendNotificationAsync(new Application.DTOs.Notifications.CreateNotificationDto
        {
            UserId = shop.OwnerUserId,
            Type = "shop_rejected",
            Title = "Hồ sơ mở gian hàng bị từ chối",
            Body = $"Hồ sơ gian hàng '{shop.Name}' chưa được phê duyệt. Lý do: {request.ReasonNote}",
            DataJson = JsonSerializer.Serialize(new { ShopId = shop.Id, shop.Slug, request.ReasonCode, request.ReasonNote })
        }, cancellationToken);

        _logger.LogInformation("Admin {AdminId} đã từ chối gian hàng {ShopId} ({Slug}). Lý do: {Reason}", adminId, shop.Id, shop.Slug, request.ReasonNote);

        return await GetShopDetailAsync(id, cancellationToken);
    }

    public async Task<ShopSanctionDto> AddSanctionAsync(Guid id, CreateSanctionRequestDto request, CancellationToken cancellationToken = default)
    {
        var adminId = GetCurrentAdminId();

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ValidationAppException("Reason", "Lý do áp dụng chế tài không được để trống.");
        }

        var shop = await _unitOfWork.Shops.GetByIdAsync(id, cancellationToken);
        if (shop == null) throw new NotFoundException("Shop", id);

        var sanction = new ShopSanction(
            shop.Id,
            request.Type,
            request.Reason,
            adminId,
            startAtUtc: _clock.UtcNow,
            endAtUtc: request.EndAtUtc);

        await _unitOfWork.ShopSanctions.AddAsync(sanction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove(CacheKeys.ShopPublic(shop.Slug));

        await _notificationService.SendNotificationAsync(new Application.DTOs.Notifications.CreateNotificationDto
        {
            UserId = shop.OwnerUserId,
            Type = "shop_sanctioned",
            Title = "Gian hàng nhận quyết định xử phạt",
            Body = $"Gian hàng '{shop.Name}' bị áp dụng chế tài {request.Type}. Lý do: {request.Reason}",
            DataJson = JsonSerializer.Serialize(new { ShopId = shop.Id, SanctionId = sanction.Id, request.Type, request.Reason })
        }, cancellationToken);

        _logger.LogWarning("Admin {AdminId} đã áp dụng chế tài {Type} lên gian hàng {ShopId}. Lý do: {Reason}", adminId, request.Type, shop.Id, request.Reason);

        return new ShopSanctionDto
        {
            Id = sanction.Id,
            Type = sanction.Type.ToString(),
            Reason = sanction.Reason,
            StartAtUtc = sanction.StartAtUtc,
            EndAtUtc = sanction.EndAtUtc,
            IsActive = true,
            LiftedAtUtc = null
        };
    }

    public async Task<bool> LiftSanctionAsync(Guid id, Guid sanctionId, CancellationToken cancellationToken = default)
    {
        var adminId = GetCurrentAdminId();

        var shop = await _unitOfWork.Shops.GetByIdAsync(id, cancellationToken);
        if (shop == null) throw new NotFoundException("Shop", id);

        var sanction = await _unitOfWork.ShopSanctions.GetByIdAsync(sanctionId, cancellationToken);
        if (sanction == null || sanction.ShopId != id)
        {
            throw new NotFoundException("Quyết định xử phạt không tồn tại hoặc không thuộc gian hàng này.");
        }

        sanction.Lift(adminId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove(CacheKeys.ShopPublic(shop.Slug));

        await _notificationService.SendNotificationAsync(new Application.DTOs.Notifications.CreateNotificationDto
        {
            UserId = shop.OwnerUserId,
            Type = "shop_sanction_lifted",
            Title = "Quyết định xử phạt đã được gỡ bỏ",
            Body = $"Quyết định xử phạt đối với gian hàng '{shop.Name}' đã được gỡ bỏ bởi ban quản trị.",
            DataJson = JsonSerializer.Serialize(new { ShopId = shop.Id, SanctionId = sanction.Id })
        }, cancellationToken);

        _logger.LogInformation("Admin {AdminId} đã gỡ bỏ chế tài {SanctionId} cho gian hàng {ShopId}", adminId, sanctionId, id);

        return true;
    }

    private Guid GetCurrentAdminId()
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
