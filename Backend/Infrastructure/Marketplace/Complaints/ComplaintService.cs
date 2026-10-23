using Application.Common;
using Application.DTOs.Marketplace.Complaints;
using Application.DTOs.Marketplace.Ledger;
using Application.DTOs.Marketplace.Products;
using Application.DTOs.Notifications;
using Application.Interfaces;
using Application.Interfaces.Marketplace.Complaints;
using Application.Interfaces.Marketplace.Ledger;
using Application.Interfaces.Repositories;
using Domain.Entities.Marketplace.Complaints;
using Domain.Entities.Marketplace.Payments;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Complaints;

public class ComplaintService : IComplaintService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILedgerService _ledgerService;
    private readonly INotificationService _notificationService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ComplaintService> _logger;

    public ComplaintService(
        IUnitOfWork unitOfWork,
        ILedgerService ledgerService,
        INotificationService notificationService,
        ICacheService cacheService,
        ILogger<ComplaintService> logger)
    {
        _unitOfWork = unitOfWork;
        _ledgerService = ledgerService;
        _notificationService = notificationService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<ComplaintDetailDto> CreateComplaintAsync(
        Guid buyerUserId,
        Guid orderId,
        Guid orderItemId,
        CreateComplaintRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, cancellationToken);
        if (order == null || order.BuyerUserId != buyerUserId)
        {
            throw new KeyNotFoundException("Không tìm thấy đơn hàng hợp lệ.");
        }

        if (order.Source != OrderSource.Marketplace)
        {
            throw new InvalidOperationException("Chỉ có thể gửi khiếu nại trên sàn đối với các đơn hàng mua trực tiếp qua hệ thống.");
        }

        if (order.Status != OrderStatus.Delivered)
        {
            throw new InvalidOperationException("Chỉ có thể gửi khiếu nại cho đơn hàng đang trong thời gian bảo hành sau giao hàng (Delivered).");
        }

        if (order.ComplaintWindowEndsAtUtc.HasValue && order.ComplaintWindowEndsAtUtc.Value < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Đơn hàng đã hết thời hạn khiếu nại (48 giờ kể từ khi giao hàng).");
        }

        var orderItem = order.Items.FirstOrDefault(i => i.Id == orderItemId);
        if (orderItem == null)
        {
            throw new KeyNotFoundException("Không tìm thấy sản phẩm trong đơn hàng.");
        }

        if (request.FaultyQuantity <= 0 || request.FaultyQuantity > (orderItem.Quantity - orderItem.RefundedQty))
        {
            throw new ArgumentOutOfRangeException(nameof(request.FaultyQuantity), "Số lượng lỗi khiếu nại không hợp lệ.");
        }

        if (request.EvidenceUrls == null || request.EvidenceUrls.Count == 0)
        {
            throw new ArgumentException("Bắt buộc phải cung cấp ít nhất một bằng chứng (ảnh hoặc video) khi khiếu nại.", nameof(request.EvidenceUrls));
        }

        var hasActive = await _unitOfWork.Complaints.HasActiveComplaintForOrderItemAsync(orderItemId, cancellationToken);
        if (hasActive)
        {
            throw new InvalidOperationException("Món hàng này đang có một khiếu nại mở đang được thụ lý.");
        }

        var complaint = new Complaint(
            order.Id,
            orderItem.Id,
            buyerUserId,
            order.ShopId,
            request.Reason,
            request.Description,
            request.FaultyQuantity,
            24); // Shop có 24 giờ phản hồi

        foreach (var url in request.EvidenceUrls)
        {
            complaint.AddEvidence(new ComplaintEvidence(complaint.Id, url, "Image"));
        }

        await _unitOfWork.Complaints.AddAsync(complaint, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Gửi thông báo tới chủ Shop
        var shop = await _unitOfWork.Shops.GetByIdAsync(order.ShopId, cancellationToken);
        if (shop != null)
        {
            try
            {
                await _notificationService.SendNotificationAsync(new CreateNotificationDto
                {
                    UserId = shop.OwnerUserId,
                    Type = "ComplaintCreated",
                    Title = "Khiếu nại đơn hàng mới",
                    Body = $"Đơn hàng #{order.OrderNo} nhận được khiếu nại cho món '{orderItem.ProductName}'. Vui lòng phản hồi trong vòng 24 giờ."
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi gửi thông báo khiếu nại tới Shop {ShopId}", order.ShopId);
            }
        }

        return MapToDetailDto(complaint, order.OrderNo, orderItem.ProductName, orderItem.VariantLabel, orderItem.UnitPrice, orderItem.Quantity);
    }

    public async Task<PagedResult<ComplaintListItemDto>> GetBuyerComplaintsAsync(
        Guid buyerUserId,
        ComplaintStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var (items, totalCount) = await _unitOfWork.Complaints.GetBuyerComplaintsAsync(buyerUserId, status, page, pageSize, cancellationToken);

        var dtos = items.Select(c => new ComplaintListItemDto
        {
            Id = c.Id,
            ShopOrderId = c.ShopOrderId,
            OrderNo = c.ShopOrder?.OrderNo ?? string.Empty,
            OrderItemId = c.OrderItemId,
            ProductName = c.OrderItem?.ProductName ?? string.Empty,
            VariantLabel = c.OrderItem?.VariantLabel ?? string.Empty,
            FaultyQuantity = c.FaultyQuantity,
            Reason = c.Reason,
            Status = c.Status,
            ShopDeadlineAt = c.ShopDeadlineAtUtc,
            CreatedAt = c.CreatedAtUtc
        }).ToList();

        return new PagedResult<ComplaintListItemDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ComplaintDetailDto?> GetBuyerComplaintByIdAsync(
        Guid buyerUserId,
        Guid complaintId,
        CancellationToken cancellationToken = default)
    {
        var complaint = await _unitOfWork.Complaints.GetByIdAsync(complaintId, cancellationToken);
        if (complaint == null || complaint.BuyerUserId != buyerUserId) return null;

        return MapToDetailDto(
            complaint,
            complaint.ShopOrder?.OrderNo ?? string.Empty,
            complaint.OrderItem?.ProductName ?? string.Empty,
            complaint.OrderItem?.VariantLabel ?? string.Empty,
            complaint.OrderItem?.UnitPrice ?? 0,
            complaint.OrderItem?.Quantity ?? 0);
    }

    public async Task CancelComplaintAsync(
        Guid buyerUserId,
        Guid complaintId,
        CancellationToken cancellationToken = default)
    {
        var complaint = await _unitOfWork.Complaints.GetByIdAsync(complaintId, cancellationToken);
        if (complaint == null || complaint.BuyerUserId != buyerUserId)
        {
            throw new KeyNotFoundException("Không tìm thấy khiếu nại hợp lệ.");
        }

        complaint.BuyerCancel();
        _unitOfWork.Complaints.Update(complaint);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<ComplaintListItemDto>> GetShopComplaintsAsync(
        Guid sellerUserId,
        ComplaintStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(sellerUserId, cancellationToken);
        if (shop == null) throw new InvalidOperationException("Người dùng chưa mở gian hàng.");

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var (items, totalCount) = await _unitOfWork.Complaints.GetShopComplaintsAsync(shop.Id, status, page, pageSize, cancellationToken);

        var dtos = items.Select(c => new ComplaintListItemDto
        {
            Id = c.Id,
            ShopOrderId = c.ShopOrderId,
            OrderNo = c.ShopOrder?.OrderNo ?? string.Empty,
            OrderItemId = c.OrderItemId,
            ProductName = c.OrderItem?.ProductName ?? string.Empty,
            VariantLabel = c.OrderItem?.VariantLabel ?? string.Empty,
            FaultyQuantity = c.FaultyQuantity,
            Reason = c.Reason,
            Status = c.Status,
            ShopDeadlineAt = c.ShopDeadlineAtUtc,
            CreatedAt = c.CreatedAtUtc
        }).ToList();

        return new PagedResult<ComplaintListItemDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task RespondComplaintAsync(
        Guid sellerUserId,
        Guid complaintId,
        ShopRespondComplaintRequest request,
        CancellationToken cancellationToken = default)
    {
        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(sellerUserId, cancellationToken);
        if (shop == null) throw new InvalidOperationException("Người dùng chưa mở gian hàng.");

        var complaint = await _unitOfWork.Complaints.GetByIdAsync(complaintId, cancellationToken);
        if (complaint == null || complaint.ShopId != shop.Id)
        {
            throw new KeyNotFoundException("Không tìm thấy khiếu nại của gian hàng.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (request.Accept)
            {
                if (string.Equals(request.ResolutionType, "Refund", StringComparison.OrdinalIgnoreCase))
                {
                    complaint.ShopAccept(request.Note, "Refund");
                    await ProcessRefundAsync(complaint, RefundBearer.Shop, request.Note ?? string.Empty, cancellationToken);
                }
                else
                {
                    complaint.ShopAccept(request.Note, "Replacement");
                }
            }
            else
            {
                complaint.ShopReject(request.Note);
            }

            _unitOfWork.Complaints.Update(complaint);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        // Thông báo cho Buyer
        try
        {
            await _notificationService.SendNotificationAsync(new CreateNotificationDto
            {
                UserId = complaint.BuyerUserId,
                Type = "ComplaintResponded",
                Title = request.Accept ? "Khiếu nại được chấp thuận" : "Shop phản hồi khiếu nại",
                Body = request.Accept
                    ? $"Gian hàng đã đồng ý giải quyết khiếu nại của bạn theo phương thức {request.ResolutionType}."
                    : $"Gian hàng đã phản hồi từ chối khiếu nại. Hồ sơ đã được chuyển lên Quản trị viên để phân xử công bằng."
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi gửi thông báo phản hồi khiếu nại tới Buyer {BuyerUserId}", complaint.BuyerUserId);
        }
    }

    public async Task<PagedResult<ComplaintListItemDto>> GetAdminComplaintsAsync(
        ComplaintStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var (items, totalCount) = await _unitOfWork.Complaints.GetAdminComplaintsAsync(status, page, pageSize, cancellationToken);

        var dtos = items.Select(c => new ComplaintListItemDto
        {
            Id = c.Id,
            ShopOrderId = c.ShopOrderId,
            OrderNo = c.ShopOrder?.OrderNo ?? string.Empty,
            OrderItemId = c.OrderItemId,
            ProductName = c.OrderItem?.ProductName ?? string.Empty,
            VariantLabel = c.OrderItem?.VariantLabel ?? string.Empty,
            FaultyQuantity = c.FaultyQuantity,
            Reason = c.Reason,
            Status = c.Status,
            ShopDeadlineAt = c.ShopDeadlineAtUtc,
            CreatedAt = c.CreatedAtUtc
        }).ToList();

        return new PagedResult<ComplaintListItemDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task ResolveComplaintAsync(
        Guid adminId,
        Guid complaintId,
        AdminResolveComplaintRequest request,
        CancellationToken cancellationToken = default)
    {
        var complaint = await _unitOfWork.Complaints.GetByIdAsync(complaintId, cancellationToken);
        if (complaint == null) throw new KeyNotFoundException("Không tìm thấy khiếu nại.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (string.Equals(request.Resolution, "Refund", StringComparison.OrdinalIgnoreCase))
            {
                complaint.AdminResolve(adminId, ComplaintStatus.ResolvedRefund, "Refund", request.Note);
                await ProcessRefundAsync(complaint, request.Bearer, request.Note, cancellationToken);
            }
            else if (string.Equals(request.Resolution, "Replacement", StringComparison.OrdinalIgnoreCase))
            {
                complaint.AdminResolve(adminId, ComplaintStatus.ResolvedReplacement, "Replacement", request.Note);
            }
            else
            {
                complaint.AdminResolve(adminId, ComplaintStatus.Rejected, "Rejected", request.Note);
            }

            _unitOfWork.Complaints.Update(complaint);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        // Thông báo phán quyết tới 2 bên
        try
        {
            var msg = $"Khiếu nại đơn #{complaint.ShopOrder?.OrderNo} đã có phán quyết từ Admin: {request.Resolution}. Lý do: {request.Note}";
            await _notificationService.SendNotificationAsync(new CreateNotificationDto
            {
                UserId = complaint.BuyerUserId,
                Type = "ComplaintResolved",
                Title = "Phán quyết khiếu nại từ Quản trị viên",
                Body = msg
            }, cancellationToken);

            var shop = await _unitOfWork.Shops.GetByIdAsync(complaint.ShopId, cancellationToken);
            if (shop != null)
            {
                await _notificationService.SendNotificationAsync(new CreateNotificationDto
                {
                    UserId = shop.OwnerUserId,
                    Type = "ComplaintResolved",
                    Title = "Phán quyết khiếu nại từ Quản trị viên",
                    Body = msg
                }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi gửi thông báo phán quyết khiếu nại.");
        }
    }

    private async Task ProcessRefundAsync(
        Complaint complaint,
        RefundBearer bearer,
        string note,
        CancellationToken cancellationToken)
    {
        var order = complaint.ShopOrder ?? await _unitOfWork.Orders.GetByIdAsync(complaint.ShopOrderId, cancellationToken);
        var orderItem = complaint.OrderItem ?? order?.Items.FirstOrDefault(i => i.Id == complaint.OrderItemId);

        if (order == null || orderItem == null)
        {
            throw new InvalidOperationException("Không thể xác định thông tin đơn hàng để hoàn tiền.");
        }

        var payment = await _unitOfWork.Payments.GetByCheckoutIdAsync(order.CheckoutId, cancellationToken);
        if (payment == null)
        {
            throw new InvalidOperationException("Không tìm thấy thông tin thanh toán gốc của đơn hàng.");
        }

        var refundAmount = orderItem.UnitPrice * complaint.FaultyQuantity;
        long commissionPart = (long)Math.Round((double)refundAmount * orderItem.CommissionRateBp / 10000.0);
        long shopPart = refundAmount - commissionPart;

        long shopDeduction = 0;
        long platformDeduction = 0;

        if (bearer == RefundBearer.Shop)
        {
            shopDeduction = shopPart;
            platformDeduction = commissionPart; // Sàn hoàn lại phần phí hoa hồng tương ứng
        }
        else if (bearer == RefundBearer.Platform)
        {
            shopDeduction = 0;
            platformDeduction = refundAmount; // Sàn chịu toàn bộ (lỗi hệ thống)
        }
        else
        {
            shopDeduction = shopPart / 2;
            platformDeduction = refundAmount - shopDeduction;
        }

        // Kiểm tra và khấu trừ số dư Shop trước để đảm bảo đủ số dư
        long deductFromPending = 0;
        long deductFromAvailable = 0;
        if (shopDeduction > 0)
        {
            var balanceCache = await _unitOfWork.Ledgers.GetOrCreateBalanceCacheAsync(order.ShopId, cancellationToken);
            deductFromPending = Math.Min(balanceCache.PendingBalance, shopDeduction);
            deductFromAvailable = shopDeduction - deductFromPending;

            balanceCache.DeductRefund(shopDeduction);
            _unitOfWork.Ledgers.UpdateBalanceCache(balanceCache);
        }

        var refund = new Refund(
            payment.Id,
            order.Id,
            refundAmount,
            bearer,
            shopDeduction,
            platformDeduction,
            orderItem.Id,
            complaint.Id,
            complaint.Reason.ToString());

        await _unitOfWork.Refunds.AddAsync(refund, cancellationToken);

        // Ghi sổ cái kép khi hoàn tiền (M7.3) - Đồng bộ chính xác giữa Pending và Available
        var platformRevenue = await _unitOfWork.Ledgers.GetOrCreateSystemAccountAsync(LedgerAccountType.PlatformRevenue, cancellationToken);
        var refundPayable = await _unitOfWork.Ledgers.GetOrCreateSystemAccountAsync(LedgerAccountType.RefundPayable, cancellationToken);

        var lines = new List<LedgerPostingLine>();

        if (deductFromPending > 0)
        {
            var shopPending = await _unitOfWork.Ledgers.GetOrCreateShopAccountAsync(order.ShopId, LedgerAccountType.ShopPending, cancellationToken);
            lines.Add(new LedgerPostingLine { AccountId = shopPending.Id, Debit = deductFromPending, Credit = 0 });
        }
        if (deductFromAvailable > 0)
        {
            var shopAvailable = await _unitOfWork.Ledgers.GetOrCreateShopAccountAsync(order.ShopId, LedgerAccountType.ShopAvailable, cancellationToken);
            lines.Add(new LedgerPostingLine { AccountId = shopAvailable.Id, Debit = deductFromAvailable, Credit = 0 });
        }
        if (platformDeduction > 0)
        {
            lines.Add(new LedgerPostingLine { AccountId = platformRevenue.Id, Debit = platformDeduction, Credit = 0 });
        }
        lines.Add(new LedgerPostingLine { AccountId = refundPayable.Id, Debit = 0, Credit = refundAmount });

        // Cập nhật số lượng đã hoàn trên OrderItem
        orderItem.AddRefundedQty(complaint.FaultyQuantity);

        await _ledgerService.PostTransactionAsync(
            LedgerTransactionType.RefundProcessed,
            "Refund",
            refund.Id,
            $"refund:{refund.Id}",
            $"Hoàn tiền khiếu nại #{complaint.Id} cho đơn #{order.OrderNo} ({complaint.FaultyQuantity} món lỗi)",
            lines,
            cancellationToken);

        _cacheService.Remove($"shop:{order.ShopId}:balance");
    }

    private static ComplaintDetailDto MapToDetailDto(
        Complaint c,
        string orderNo,
        string productName,
        string variantLabel,
        long unitPrice,
        int quantityBought)
    {
        return new ComplaintDetailDto
        {
            Id = c.Id,
            ShopOrderId = c.ShopOrderId,
            OrderNo = orderNo,
            OrderItemId = c.OrderItemId,
            ProductName = productName,
            VariantLabel = variantLabel,
            UnitPrice = unitPrice,
            QuantityBought = quantityBought,
            FaultyQuantity = c.FaultyQuantity,
            Reason = c.Reason,
            Description = c.Description,
            Status = c.Status,
            ShopDeadlineAt = c.ShopDeadlineAtUtc,
            ShopResponseNote = c.ShopResponseNote,
            ShopRespondedAt = c.ShopRespondedAtUtc,
            AdminResolutionNote = c.AdminResolutionNote,
            ResolutionType = c.ResolutionType,
            ResolvedAt = c.ResolvedAtUtc,
            Evidences = c.Evidences.Select(e => new ComplaintEvidenceDto
            {
                Id = e.Id,
                FileUrl = e.FileUrl,
                FileType = e.FileType,
                Note = e.Note
            }).ToList(),
            CreatedAt = c.CreatedAtUtc
        };
    }
}
