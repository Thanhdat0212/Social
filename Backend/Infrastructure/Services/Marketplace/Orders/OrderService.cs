using Application.Common;
using Application.DTOs.Marketplace.Orders;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Orders;
using Application.Interfaces.Repositories;
using Domain.Entities.Marketplace.Orders;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Marketplace.Orders;

public class OrderService : IOrderService
{
    private readonly SocialDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICryptoService _cryptoService;
    private readonly ISignedUrlService _signedUrlService;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        SocialDbContext context,
        IUnitOfWork unitOfWork,
        ICryptoService cryptoService,
        ISignedUrlService signedUrlService,
        ILogger<OrderService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _cryptoService = cryptoService;
        _signedUrlService = signedUrlService;
        _logger = logger;
    }

    public async Task<PagedResult<BuyerOrderDetailDto>> GetBuyerOrdersAsync(
        Guid buyerUserId,
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var p = page > 0 ? page : 1;
        var ps = pageSize > 0 ? pageSize : 20;

        var orders = await _unitOfWork.Orders.GetBuyerOrdersAsync(buyerUserId, status, p, ps, cancellationToken);
        var totalCount = await _unitOfWork.Orders.GetBuyerOrdersCountAsync(buyerUserId, status, cancellationToken);

        var items = orders.Select(o => new BuyerOrderDetailDto
        {
            Id = o.Id,
            OrderNo = o.OrderNo,
            ShopId = o.ShopId,
            ShopName = o.Shop?.Name ?? string.Empty,
            ShopSlug = o.Shop?.Slug ?? string.Empty,
            Status = o.Status.ToString(),
            Total = o.Total,
            DeliveredAtUtc = o.DeliveredAtUtc,
            ComplaintWindowEndsAtUtc = o.ComplaintWindowEndsAtUtc,
            CreatedAtUtc = o.CreatedAtUtc,
            Items = o.Items.Select(i => new BuyerOrderItemDto
            {
                Id = i.Id,
                VariantId = i.VariantId,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                VariantLabel = i.VariantLabel,
                Sku = i.Sku,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                LineTotal = i.LineTotal,
                DeliveryType = i.DeliveryType.ToString()
            }).ToList()
        }).ToList();

        return new PagedResult<BuyerOrderDetailDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = p,
            PageSize = ps
        };
    }

    public async Task<BuyerOrderDetailDto?> GetBuyerOrderByIdAsync(
        Guid buyerUserId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, cancellationToken);
        if (order == null || order.BuyerUserId != buyerUserId)
        {
            return null;
        }

        var itemDtos = new List<BuyerOrderItemDto>();

        foreach (var item in order.Items)
        {
            var deliveredCodes = new List<string>();
            string? downloadFileName = null;
            string? downloadStoragePath = null;

            // Nếu là mã / tài khoản: Giải mã AES-GCM văn bản gốc cho người mua
            if (item.DeliveryType == DeliveryType.CodePool || item.DeliveryType == DeliveryType.Course)
            {
                var codes = await _context.DigitalCodes
                    .Where(c => c.OrderItemId == item.Id && c.Status == DigitalCodeStatus.Delivered)
                    .Select(c => c.ValueEncrypted)
                    .ToListAsync(cancellationToken);

                foreach (var enc in codes)
                {
                    try
                    {
                        var decrypted = _cryptoService.Decrypt(enc);
                        deliveredCodes.Add(decrypted);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi giải mã mã số cho OrderItem {OrderItemId}", item.Id);
                        deliveredCodes.Add("[Lỗi giải mã - Vui lòng liên hệ hỗ trợ]");
                    }
                }
            }
            else if (item.DeliveryType == DeliveryType.File)
            {
                var file = await _unitOfWork.DigitalFiles.GetByVariantIdAsync(item.VariantId, cancellationToken);
                if (file != null)
                {
                    downloadFileName = file.FileName;
                    downloadStoragePath = file.StoragePath;
                }
            }

            int downloadCount = item.DownloadCount;
            int? maxDownloads = null;
            string? signedDownloadUrl = null;

            if (item.DeliveryType == DeliveryType.File)
            {
                var file = await _unitOfWork.DigitalFiles.GetByVariantIdAsync(item.VariantId, cancellationToken);
                if (file != null)
                {
                    maxDownloads = file.MaxDownloads;
                    signedDownloadUrl = _signedUrlService.BuildDownloadUrl(
                        string.Empty,
                        item.Id,
                        order.BuyerUserId,
                        TimeSpan.FromMinutes(30));
                }
            }

            itemDtos.Add(new BuyerOrderItemDto
            {
                Id = item.Id,
                VariantId = item.VariantId,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                VariantLabel = item.VariantLabel,
                Sku = item.Sku,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                LineTotal = item.LineTotal,
                DeliveryType = item.DeliveryType.ToString(),
                DeliveredCodes = deliveredCodes,
                DownloadFileName = downloadFileName,
                DownloadStoragePath = downloadStoragePath,
                SignedDownloadUrl = signedDownloadUrl,
                DownloadCount = downloadCount,
                MaxDownloads = maxDownloads
            });
        }

        return new BuyerOrderDetailDto
        {
            Id = order.Id,
            OrderNo = order.OrderNo,
            ShopId = order.ShopId,
            ShopName = order.Shop?.Name ?? string.Empty,
            ShopSlug = order.Shop?.Slug ?? string.Empty,
            Status = order.Status.ToString(),
            Total = order.Total,
            DeliveredAtUtc = order.DeliveredAtUtc,
            ComplaintWindowEndsAtUtc = order.ComplaintWindowEndsAtUtc,
            CreatedAtUtc = order.CreatedAtUtc,
            Items = itemDtos
        };
    }

    public async Task<PagedResult<SellerOrderDetailDto>> GetShopOrdersAsync(
        Guid sellerUserId,
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(sellerUserId, cancellationToken);
        if (shop == null)
        {
            throw new KeyNotFoundException("Bạn chưa đăng ký gian hàng.");
        }

        var p = page > 0 ? page : 1;
        var ps = pageSize > 0 ? pageSize : 20;

        var orders = await _unitOfWork.Orders.GetShopOrdersAsync(shop.Id, status, p, ps, cancellationToken);
        var totalCount = await _unitOfWork.Orders.GetShopOrdersCountAsync(shop.Id, status, cancellationToken);

        var items = orders.Select(o => new SellerOrderDetailDto
        {
            Id = o.Id,
            OrderNo = o.OrderNo,
            BuyerUserId = o.BuyerUserId,
            Status = o.Status.ToString(),
            Total = o.Total,
            ShopGross = o.ShopGross,
            CommissionTotal = o.CommissionTotal,
            ShopNet = o.ShopNet,
            DeliveredAtUtc = o.DeliveredAtUtc,
            CreatedAtUtc = o.CreatedAtUtc,
            Items = o.Items.Select(i => new SellerOrderItemDto
            {
                Id = i.Id,
                VariantId = i.VariantId,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                VariantLabel = i.VariantLabel,
                Sku = i.Sku,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                LineTotal = i.LineTotal,
                ShopNet = i.ShopNet,
                DeliveryType = i.DeliveryType.ToString()
            }).ToList()
        }).ToList();

        return new PagedResult<SellerOrderDetailDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = p,
            PageSize = ps
        };
    }

    public async Task<SellerOrderDetailDto?> GetShopOrderByIdAsync(
        Guid sellerUserId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(sellerUserId, cancellationToken);
        if (shop == null)
        {
            throw new KeyNotFoundException("Bạn chưa đăng ký gian hàng.");
        }

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, cancellationToken);
        if (order == null || order.ShopId != shop.Id)
        {
            return null;
        }

        return new SellerOrderDetailDto
        {
            Id = order.Id,
            OrderNo = order.OrderNo,
            BuyerUserId = order.BuyerUserId,
            Status = order.Status.ToString(),
            Total = order.Total,
            ShopGross = order.ShopGross,
            CommissionTotal = order.CommissionTotal,
            ShopNet = order.ShopNet,
            DeliveredAtUtc = order.DeliveredAtUtc,
            CreatedAtUtc = order.CreatedAtUtc,
            Items = order.Items.Select(i => new SellerOrderItemDto
            {
                Id = i.Id,
                VariantId = i.VariantId,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                VariantLabel = i.VariantLabel,
                Sku = i.Sku,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                LineTotal = i.LineTotal,
                ShopNet = i.ShopNet,
                DeliveryType = i.DeliveryType.ToString()
            }).ToList()
        };
    }

    public async Task<SignedDownloadUrlDto> GenerateSignedDownloadUrlAsync(
        Guid buyerUserId,
        Guid orderItemId,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetByOrderItemIdAsync(orderItemId, cancellationToken);
        if (order == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin đơn hàng.");
        }

        if (order.BuyerUserId != buyerUserId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền truy cập tệp tin của đơn hàng này.");
        }

        if (order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Completed)
        {
            throw new InvalidOperationException("Chỉ có thể tải tệp tin cho đơn hàng đã được giao nhận thành công.");
        }

        var item = order.Items.FirstOrDefault(i => i.Id == orderItemId);
        if (item == null || item.DeliveryType != DeliveryType.File)
        {
            throw new InvalidOperationException("Mặt hàng này không phải là tệp tin số.");
        }

        var file = await _unitOfWork.DigitalFiles.GetByVariantIdAsync(item.VariantId, cancellationToken);
        if (file == null)
        {
            throw new KeyNotFoundException("Tệp tin chưa được cấu hình cho sản phẩm này.");
        }

        if (file.MaxDownloads.HasValue && item.DownloadCount >= file.MaxDownloads.Value)
        {
            throw new InvalidOperationException($"Bạn đã đạt tối đa số lần tải cho phép ({file.MaxDownloads.Value} lần).");
        }

        var validity = TimeSpan.FromMinutes(30);
        var expiresAtUtc = DateTime.UtcNow.Add(validity);
        var downloadUrl = _signedUrlService.BuildDownloadUrl(baseUrl, item.Id, buyerUserId, validity);

        return new SignedDownloadUrlDto
        {
            OrderItemId = item.Id,
            FileName = file.FileName,
            DownloadUrl = downloadUrl,
            ExpiresAtUtc = expiresAtUtc,
            DownloadCount = item.DownloadCount,
            MaxDownloads = file.MaxDownloads
        };
    }

    public async Task<(string storagePath, string fileName, string contentType)> ProcessFileDownloadAsync(
        Guid orderItemId,
        Guid buyerUserId,
        long expiresAt,
        string signature,
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra chữ ký HMAC
        var isValid = _signedUrlService.VerifySignature(orderItemId, buyerUserId, expiresAt, signature);
        if (!isValid)
        {
            throw new UnauthorizedAccessException("Chữ ký bảo mật của liên kết tải không hợp lệ hoặc đã bị thay đổi.");
        }

        // 2. Kiểm tra thời hạn hết hạn
        var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (nowUnix > expiresAt)
        {
            throw new InvalidOperationException("Liên kết tải tệp tin này đã hết hạn. Vui lòng lấy liên kết mới từ trang chi tiết đơn hàng.");
        }

        // 3. Kiểm tra đơn hàng & quyền sở hữu
        var order = await _unitOfWork.Orders.GetByOrderItemIdAsync(orderItemId, cancellationToken);
        if (order == null || order.BuyerUserId != buyerUserId)
        {
            throw new KeyNotFoundException("Không tìm thấy đơn hàng hợp lệ.");
        }

        if (order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Completed)
        {
            throw new InvalidOperationException("Đơn hàng chưa ở trạng thái hoàn tất giao hàng.");
        }

        var item = order.Items.FirstOrDefault(i => i.Id == orderItemId);
        if (item == null || item.DeliveryType != DeliveryType.File)
        {
            throw new InvalidOperationException("Mặt hàng không phải tệp tin số.");
        }

        var file = await _unitOfWork.DigitalFiles.GetByVariantIdAsync(item.VariantId, cancellationToken);
        if (file == null)
        {
            throw new KeyNotFoundException("Không tìm thấy tệp tin trên hệ thống.");
        }

        // 4. Kiểm tra giới hạn số lần tải
        if (file.MaxDownloads.HasValue && item.DownloadCount >= file.MaxDownloads.Value)
        {
            throw new InvalidOperationException($"Bạn đã vượt quá giới hạn tải tệp tin ({file.MaxDownloads.Value} lần).");
        }

        // 5. Tăng lượt tải và lưu vết
        item.IncrementDownloadCount();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Người mua {BuyerUserId} đã tải tệp tin {FileName} cho OrderItem {OrderItemId} (Lượt tải {Count}/{Max})",
            buyerUserId, file.FileName, item.Id, item.DownloadCount, file.MaxDownloads?.ToString() ?? "Không giới hạn");

        return (file.StoragePath, file.FileName, file.ContentType);
    }
}
