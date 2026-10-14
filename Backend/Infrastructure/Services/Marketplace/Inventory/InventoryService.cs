using Application.Common;
using Application.DTOs.Marketplace.Inventory;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Inventory;
using Application.Interfaces.Repositories;
using Domain.Entities.Marketplace.Inventory;
using Domain.Entities.Marketplace.Products;
using Domain.Entities.Marketplace.Shops;
using Domain.Enums;
using Infrastructure.Caching;
using Infrastructure.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Marketplace.Inventory;

public class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICryptoService _cryptoService;
    private readonly ICacheService _cacheService;
    private readonly IHubContext<SocialHub> _hubContext;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        IUnitOfWork unitOfWork,
        ICryptoService cryptoService,
        ICacheService cacheService,
        IHubContext<SocialHub> hubContext,
        ILogger<InventoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _cryptoService = cryptoService;
        _cacheService = cacheService;
        _hubContext = hubContext;
        _logger = logger;
    }

    private async Task<(Shop Shop, ProductVariant Variant)> GetRequiredShopAndVariantAsync(
        Guid userId,
        Guid variantId,
        CancellationToken cancellationToken)
    {
        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(userId, cancellationToken);
        if (shop == null)
        {
            throw new KeyNotFoundException("Bạn chưa đăng ký gian hàng trên hệ thống.");
        }

        var variant = await _unitOfWork.ProductVariants.GetByIdAsync(variantId, cancellationToken);
        if (variant == null || variant.ShopId != shop.Id)
        {
            throw new KeyNotFoundException("Không tìm thấy biến thể sản phẩm hoặc bạn không có quyền truy cập.");
        }

        return (shop, variant);
    }

    public async Task<BulkImportCodesResultDto> BulkImportCodesAsync(
        Guid userId,
        Guid variantId,
        BulkImportCodesRequest request,
        CancellationToken cancellationToken = default)
    {
        var (shop, variant) = await GetRequiredShopAndVariantAsync(userId, variantId, cancellationToken);

        // 1. Phân tích và gom danh sách mã đầu vào
        var rawLines = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.RawCodes))
        {
            var split = request.RawCodes.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            rawLines.AddRange(split);
        }

        if (request.Codes != null && request.Codes.Count > 0)
        {
            rawLines.AddRange(request.Codes);
        }

        // Lọc chuỗi rỗng
        var candidateList = rawLines
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        if (candidateList.Count == 0)
        {
            return new BulkImportCodesResultDto
            {
                TotalSubmitted = 0,
                SuccessfullyAdded = 0,
                DuplicatesCount = 0,
                InvalidCount = 0
            };
        }

        // Giới hạn 1000 mã / lần nạp
        const int maxBatchSize = 1000;
        if (candidateList.Count > maxBatchSize)
        {
            throw new InvalidOperationException($"Số lượng mã nạp mỗi lần tối đa là {maxBatchSize} dòng (bạn gửi {candidateList.Count} dòng).");
        }

        // 2. Kiểm tra trùng lặp nội bộ trong danh sách gửi lên (Batch In-Memory Deduplication)
        var uniqueBatchItems = new List<(string PlainText, string Hash)>();
        var duplicateSamples = new List<string>();
        var seenHashesInBatch = new HashSet<string>();

        foreach (var plain in candidateList)
        {
            var hash = _cryptoService.ComputeHash(plain);
            if (seenHashesInBatch.Contains(hash))
            {
                if (duplicateSamples.Count < 5)
                {
                    duplicateSamples.Add(_cryptoService.MaskValue(plain));
                }
            }
            else
            {
                seenHashesInBatch.Add(hash);
                uniqueBatchItems.Add((plain, hash));
            }
        }

        // 3. Đối chiếu trùng lặp với Database bằng 1 truy vấn duy nhất qua ValueHash
        var existingHashesInDb = await _unitOfWork.DigitalCodes.GetExistingHashesAsync(
            shop.Id,
            variant.Id,
            seenHashesInBatch,
            cancellationToken);

        // 4. Mã hóa và tạo thực thể cho các mã hợp lệ
        var newCodes = new List<DigitalCode>();

        foreach (var (plain, hash) in uniqueBatchItems)
        {
            if (existingHashesInDb.Contains(hash))
            {
                if (duplicateSamples.Count < 5)
                {
                    duplicateSamples.Add(_cryptoService.MaskValue(plain));
                }
            }
            else
            {
                var encrypted = _cryptoService.Encrypt(plain);
                var mask = _cryptoService.MaskValue(plain);

                newCodes.Add(new DigitalCode(shop.Id, variant.Id, encrypted, hash, mask));
            }
        }

        // 5. Lưu vào Database
        if (newCodes.Count > 0)
        {
            await _unitOfWork.DigitalCodes.AddRangeAsync(newCodes, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Xóa cache và phát WebSocket Realtime tới toàn bộ client
            await InvalidateProductCacheAndNotifyRealtimeAsync(variant.ProductId, variant.Id, cancellationToken);
        }

        var duplicatesTotal = candidateList.Count - newCodes.Count;

        _logger.LogInformation(
            "Shop {ShopId} nạp mã cho Variant {VariantId}: Nhận {Total}, Thành công {Success}, Trùng lặp {Duplicates}",
            shop.Id, variant.Id, candidateList.Count, newCodes.Count, duplicatesTotal);

        return new BulkImportCodesResultDto
        {
            TotalSubmitted = candidateList.Count,
            SuccessfullyAdded = newCodes.Count,
            DuplicatesCount = duplicatesTotal,
            InvalidCount = 0,
            DuplicateSamples = duplicateSamples
        };
    }

    public async Task<PagedResult<DigitalCodeItemDto>> GetCodesAsync(
        Guid userId,
        Guid variantId,
        DigitalCodeStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (shop, variant) = await GetRequiredShopAndVariantAsync(userId, variantId, cancellationToken);

        var p = page > 0 ? page : 1;
        var ps = pageSize > 0 ? pageSize : 20;

        var codes = await _unitOfWork.DigitalCodes.GetCodesByVariantAsync(shop.Id, variant.Id, status, p, ps, cancellationToken);
        var totalCount = await _unitOfWork.DigitalCodes.GetCodesCountByVariantAsync(shop.Id, variant.Id, status, cancellationToken);

        var items = codes.Select(c => new DigitalCodeItemDto
        {
            Id = c.Id,
            MaskedValue = c.MaskedValue,
            Status = c.Status.ToString(),
            DeliveredAtUtc = c.DeliveredAtUtc,
            CreatedAtUtc = c.CreatedAtUtc
        }).ToList();

        return new PagedResult<DigitalCodeItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = p,
            PageSize = ps
        };
    }

    public async Task<InventorySummaryDto> GetInventorySummaryAsync(
        Guid userId,
        Guid variantId,
        CancellationToken cancellationToken = default)
    {
        var (shop, variant) = await GetRequiredShopAndVariantAsync(userId, variantId, cancellationToken);
        var summary = await _unitOfWork.DigitalCodes.GetInventorySummaryAsync(variant.Id, cancellationToken);

        return new InventorySummaryDto
        {
            VariantId = variant.Id,
            Total = summary.Total,
            Available = summary.Available,
            Reserved = summary.Reserved,
            Delivered = summary.Delivered,
            Invalid = summary.Invalid
        };
    }

    public async Task InvalidateCodeAsync(
        Guid userId,
        Guid variantId,
        Guid codeId,
        CancellationToken cancellationToken = default)
    {
        var (shop, variant) = await GetRequiredShopAndVariantAsync(userId, variantId, cancellationToken);

        var code = await _unitOfWork.DigitalCodes.GetByIdAsync(codeId, cancellationToken);
        if (code == null || code.ShopId != shop.Id || code.VariantId != variant.Id)
        {
            throw new KeyNotFoundException("Không tìm thấy mã số trong kho của bạn.");
        }

        code.MarkInvalid();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await InvalidateProductCacheAndNotifyRealtimeAsync(variant.ProductId, variant.Id, cancellationToken);

        _logger.LogInformation("Shop {ShopId} đã đánh dấu Invalid cho mã {CodeId} (Variant {VariantId})", shop.Id, code.Id, variant.Id);
    }

    public async Task DeleteCodeAsync(
        Guid userId,
        Guid variantId,
        Guid codeId,
        CancellationToken cancellationToken = default)
    {
        var (shop, variant) = await GetRequiredShopAndVariantAsync(userId, variantId, cancellationToken);

        var code = await _unitOfWork.DigitalCodes.GetByIdAsync(codeId, cancellationToken);
        if (code == null || code.ShopId != shop.Id || code.VariantId != variant.Id)
        {
            throw new KeyNotFoundException("Không tìm thấy mã số trong kho của bạn.");
        }

        if (code.Status != DigitalCodeStatus.Available)
        {
            throw new InvalidOperationException($"Chỉ có thể xóa mã đang ở trạng thái Available. Mã hiện tại đang là '{code.Status}'.");
        }

        _unitOfWork.DigitalCodes.Delete(code);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await InvalidateProductCacheAndNotifyRealtimeAsync(variant.ProductId, variant.Id, cancellationToken);

        _logger.LogInformation("Shop {ShopId} đã xóa mã {CodeId} khỏi kho (Variant {VariantId})", shop.Id, code.Id, variant.Id);
    }

    private async Task InvalidateProductCacheAndNotifyRealtimeAsync(Guid productId, Guid variantId, CancellationToken cancellationToken)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
            if (product != null)
            {
                // 1. Xóa cache chi tiết sản phẩm để người mua F5 hoặc vào lại luôn thấy dữ liệu mới
                await _cacheService.RemoveAsync(CacheKeys.ProductDetail(product.Slug), cancellationToken);

                // 2. Lấy số lượng khả dụng mới nhất từ kho số
                var summary = await _unitOfWork.DigitalCodes.GetInventorySummaryAsync(variantId, cancellationToken);

                // 3. Bắn WebSocket SignalR tới toàn bộ client đang mở trang sản phẩm
                await _hubContext.Clients.All.SendAsync("ProductStockUpdated", new
                {
                    productId = product.Id,
                    productSlug = product.Slug,
                    variantId = variantId,
                    availableStock = summary.Available
                }, cancellationToken);

                _logger.LogInformation("Đã xóa cache và phát SignalR ProductStockUpdated cho Product {Slug}, Variant {VariantId}: Còn {Available} mã", product.Slug, variantId, summary.Available);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa cache hoặc gửi SignalR ProductStockUpdated cho Variant {VariantId}", variantId);
        }
    }

    public async Task<DigitalFileDetailDto> ConfigureDigitalFileAsync(
        Guid userId,
        Guid variantId,
        ConfigureDigitalFileRequest request,
        CancellationToken cancellationToken = default)
    {
        var (shop, variant) = await GetRequiredShopAndVariantAsync(userId, variantId, cancellationToken);

        var existingFile = await _unitOfWork.DigitalFiles.GetByVariantIdAsync(variant.Id, cancellationToken);
        if (existingFile != null)
        {
            existingFile.UpdateFile(
                request.FileName,
                request.StoragePath,
                request.FileSizeBytes,
                request.ContentType ?? "application/octet-stream",
                request.MaxDownloads);

            _unitOfWork.DigitalFiles.Update(existingFile);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return MapFileToDto(existingFile);
        }
        else
        {
            var newFile = new DigitalFile(
                shop.Id,
                variant.Id,
                request.FileName,
                request.StoragePath,
                request.FileSizeBytes,
                request.ContentType ?? "application/octet-stream",
                request.MaxDownloads);

            await _unitOfWork.DigitalFiles.AddAsync(newFile, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return MapFileToDto(newFile);
        }
    }

    public async Task<DigitalFileDetailDto?> GetDigitalFileAsync(
        Guid userId,
        Guid variantId,
        CancellationToken cancellationToken = default)
    {
        var (shop, variant) = await GetRequiredShopAndVariantAsync(userId, variantId, cancellationToken);
        var file = await _unitOfWork.DigitalFiles.GetByVariantIdAsync(variant.Id, cancellationToken);
        return file != null ? MapFileToDto(file) : null;
    }

    private static DigitalFileDetailDto MapFileToDto(DigitalFile file)
    {
        return new DigitalFileDetailDto
        {
            Id = file.Id,
            VariantId = file.VariantId,
            FileName = file.FileName,
            StoragePath = file.StoragePath,
            FileSizeBytes = file.FileSizeBytes,
            ContentType = file.ContentType,
            MaxDownloads = file.MaxDownloads,
            UpdatedAtUtc = file.UpdatedAtUtc
        };
    }
}
