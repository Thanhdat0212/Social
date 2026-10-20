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
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Shops;

public class AdminShopService : IAdminShopService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly SocialDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notificationService;
    private readonly ICacheService _cacheService;
    private readonly IClock _clock;
    private readonly ILogger<AdminShopService> _logger;

    public AdminShopService(
        IUnitOfWork unitOfWork,
        SocialDbContext dbContext,
        ICurrentUserService currentUserService,
        INotificationService notificationService,
        ICacheService cacheService,
        IClock clock,
        ILogger<AdminShopService> logger)
    {
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
        _cacheService = cacheService;
        _clock = clock;
        _logger = logger;
    }

    public async Task<(IReadOnlyList<ShopSummaryDto> Items, int Total, AdminShopListStatsDto Stats)> GetShopsAsync(
        ApprovalStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var shops = await _unitOfWork.Shops.GetShopsAsync(status, search, page, pageSize, cancellationToken);
        var total = await _unitOfWork.Shops.GetTotalCountAsync(status, search, cancellationToken);

        // Global stats across marketplace
        var totalShops = await _dbContext.Shops.CountAsync(s => s.DeletedAtUtc == null, cancellationToken);
        var approvedShops = await _dbContext.Shops.CountAsync(s => s.DeletedAtUtc == null && s.ApprovalStatus == ApprovalStatus.Approved, cancellationToken);
        var pendingShops = await _dbContext.Shops.CountAsync(s => s.DeletedAtUtc == null && s.ApprovalStatus == ApprovalStatus.Pending, cancellationToken);
        var rejectedShops = await _dbContext.Shops.CountAsync(s => s.DeletedAtUtc == null && s.ApprovalStatus == ApprovalStatus.Rejected, cancellationToken);
        var totalOrdersSold = await _dbContext.ShopOrders.CountAsync(o => o.Status != OrderStatus.Cancelled, cancellationToken);
        var totalRevenue = await _dbContext.ShopOrders.Where(o => o.Status != OrderStatus.Cancelled).SumAsync(o => (decimal?)o.Total, cancellationToken) ?? 0;

        var stats = new AdminShopListStatsDto
        {
            TotalShops = totalShops,
            ApprovedShops = approvedShops,
            PendingShops = pendingShops,
            RejectedShops = rejectedShops,
            TotalOrdersSold = totalOrdersSold,
            TotalRevenue = totalRevenue
        };

        var shopIds = shops.Select(s => s.Id).ToList();

        // Query shop-level aggregated stats (skip if 0 shops)
        var orderStats = shopIds.Count == 0
            ? new Dictionary<Guid, (int TotalOrders, decimal TotalRevenue)>()
            : (await _dbContext.ShopOrders
                .Where(o => shopIds.Contains(o.ShopId) && o.Status != OrderStatus.Cancelled)
                .GroupBy(o => o.ShopId)
                .Select(g => new
                {
                    ShopId = g.Key,
                    TotalOrders = g.Count(),
                    TotalRevenue = g.Sum(o => (decimal)o.Total)
                })
                .ToListAsync(cancellationToken))
                .ToDictionary(x => x.ShopId, x => (x.TotalOrders, x.TotalRevenue));

        var productStats = shopIds.Count == 0
            ? new Dictionary<Guid, int>()
            : (await _dbContext.Products
                .Where(p => shopIds.Contains(p.ShopId) && p.DeletedAtUtc == null)
                .GroupBy(p => p.ShopId)
                .Select(g => new
                {
                    ShopId = g.Key,
                    TotalProducts = g.Count()
                })
                .ToListAsync(cancellationToken))
                .ToDictionary(x => x.ShopId, x => x.TotalProducts);

        var balanceStats = shopIds.Count == 0
            ? new Dictionary<Guid, Domain.Entities.Marketplace.Ledger.ShopBalanceCache>()
            : await _dbContext.ShopBalanceCaches
                .Where(b => shopIds.Contains(b.ShopId))
                .ToDictionaryAsync(b => b.ShopId, cancellationToken);

        var items = shops.Select(s =>
        {
            var hasActiveSanction = s.Sanctions.Any(sn => sn.IsActive(_clock.UtcNow));
            var effectiveStatus = ShopStatusCalculator.Compute(s.OwnerUser, s, hasActiveSanction);

            orderStats.TryGetValue(s.Id, out var oStat);
            productStats.TryGetValue(s.Id, out var pCount);
            balanceStats.TryGetValue(s.Id, out var bStat);

            return new ShopSummaryDto
            {
                Id = s.Id,
                OwnerUserId = s.OwnerUserId,
                OwnerDisplayName = s.OwnerUser?.DisplayName ?? string.Empty,
                OwnerEmail = s.OwnerUser?.Email ?? string.Empty,
                Name = s.Name,
                Slug = s.Slug,
                LogoUrl = s.LogoUrl,
                ContactEmail = s.ContactEmail,
                ApprovalStatus = s.ApprovalStatus.ToString(),
                OperatingStatus = s.OperatingStatus.ToString(),
                EffectiveStatus = effectiveStatus.ToString(),
                CreatedAtUtc = s.CreatedAtUtc,
                TotalOrders = oStat.TotalOrders,
                TotalRevenue = oStat.TotalRevenue,
                TotalProducts = pCount,
                AvailableBalance = bStat?.AvailableBalance ?? 0,
                PendingBalance = bStat?.PendingBalance ?? 0
            };
        }).ToList();

        return (items, total, stats);
    }

    public async Task<ShopDetailDto> GetShopDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var shop = await _unitOfWork.Shops.GetByIdAsync(id, cancellationToken);
        if (shop == null) throw new NotFoundException("Shop", id);

        var allReviews = await _unitOfWork.ShopReviews.GetByShopIdAsync(shop.Id, cancellationToken);
        var allSanctions = await _unitOfWork.ShopSanctions.GetByShopIdAsync(shop.Id, cancellationToken);
        var latestReview = allReviews.FirstOrDefault();
        var activeSanction = allSanctions.FirstOrDefault(sn => sn.IsActive(_clock.UtcNow));

        // Additional stats & details for Admin inspection
        var totalOrders = await _dbContext.ShopOrders
            .CountAsync(o => o.ShopId == shop.Id && o.Status != OrderStatus.Cancelled, cancellationToken);
        var totalRevenue = await _dbContext.ShopOrders
            .Where(o => o.ShopId == shop.Id && o.Status != OrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.Total, cancellationToken) ?? 0;
        var totalProducts = await _dbContext.Products
            .CountAsync(p => p.ShopId == shop.Id && p.DeletedAtUtc == null, cancellationToken);

        var balance = await _dbContext.ShopBalanceCaches
            .FirstOrDefaultAsync(b => b.ShopId == shop.Id, cancellationToken);

        var bankAccount = await _dbContext.ShopBankAccounts
            .FirstOrDefaultAsync(b => b.ShopId == shop.Id && b.Status == BankAccountStatus.Verified, cancellationToken);

        var recentOrders = await _dbContext.ShopOrders
            .Include(o => o.BuyerUser)
            .Where(o => o.ShopId == shop.Id)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(10)
            .Select(o => new ShopOrderSummaryDto
            {
                Id = o.Id,
                OrderNo = o.OrderNo,
                Status = o.Status.ToString(),
                Total = o.Total,
                ShopNet = o.ShopNet,
                BuyerName = o.BuyerUser != null ? o.BuyerUser.DisplayName : "Khách hàng",
                CreatedAtUtc = o.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var recentProducts = await _dbContext.Products
            .Where(p => p.ShopId == shop.Id && p.DeletedAtUtc == null)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(10)
            .Select(p => new ShopProductSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                Price = p.MinPriceCache,
                Status = p.Status.ToString(),
                SoldCount = _dbContext.OrderItems
                    .Where(oi => oi.ProductId == p.Id && oi.ShopOrder != null && oi.ShopOrder.Status != OrderStatus.Cancelled)
                    .Sum(oi => (int?)oi.Quantity) ?? 0,
                CreatedAtUtc = p.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var dto = MapToDetailDto(shop, shop.OwnerUser, latestReview, activeSanction);
        dto.TotalOrders = totalOrders;
        dto.TotalRevenue = totalRevenue;
        dto.TotalProducts = totalProducts;
        dto.AvailableBalance = balance?.AvailableBalance ?? 0;
        dto.PendingBalance = balance?.PendingBalance ?? 0;
        dto.LifetimeGrossRevenue = balance?.LifetimeGrossRevenue ?? 0;
        dto.LifetimeCommissionPaid = balance?.LifetimeCommissionPaid ?? 0;
        dto.OwnerAvatarUrl = shop.OwnerUser?.AvatarUrl;

        if (bankAccount != null)
        {
            dto.BankAccount = new ShopBankAccountSummaryDto
            {
                Id = bankAccount.Id,
                BankCode = bankAccount.BankCode,
                BankName = bankAccount.BankName,
                AccountHolderName = bankAccount.AccountHolderName,
                MaskedAccountNumber = bankAccount.AccountNumberMasked,
                IsActive = bankAccount.Status == BankAccountStatus.Verified
            };
        }

        dto.RecentOrders = recentOrders;
        dto.RecentProducts = recentProducts;

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
        latestReview.Reject(adminId, request.ReasonCode ?? "REJECTED", request.ReasonNote, request.CanResubmit);

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
        Domain.Entities.User? user,
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
            OwnerDisplayName = user?.DisplayName ?? string.Empty,
            OwnerEmail = user?.Email ?? string.Empty,
            OwnerAvatarUrl = user?.AvatarUrl,
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
