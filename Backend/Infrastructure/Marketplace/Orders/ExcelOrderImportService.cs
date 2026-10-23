using System.Text.RegularExpressions;
using Application.Common;
using Application.DTOs.Marketplace.Orders;
using Application.Interfaces.Marketplace.Orders;
using Domain.Entities.Marketplace.Inventory;
using Domain.Entities.Marketplace.Orders;
using Domain.Entities.Marketplace.Products;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;

namespace Infrastructure.Marketplace.Orders;

public class ExcelOrderImportService : IExcelOrderImportService
{
    private readonly SocialDbContext _context;
    private readonly ILogger<ExcelOrderImportService> _logger;

    public ExcelOrderImportService(
        SocialDbContext context,
        ILogger<ExcelOrderImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ExcelOrderImportPreviewResultDto> PreviewExcelOrdersAsync(
        Guid sellerUserId,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        var shop = await _context.Shops
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.OwnerUserId == sellerUserId, cancellationToken);

        if (shop == null)
        {
            throw new InvalidOperationException("Không tìm thấy gian hàng của người bán.");
        }

        // Tải danh sách sản phẩm và biến thể của Shop để khớp SKU
        var variants = await _context.ProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
            .Where(v => v.Product != null && v.Product.ShopId == shop.Id && v.Product.Status == ProductStatus.Listed)
            .ToListAsync(cancellationToken);

        // Lấy số lượng tồn kho khả dụng của từng biến thể
        var stockCounts = await _context.DigitalCodes
            .AsNoTracking()
            .Where(c => c.ShopId == shop.Id && c.Status == DigitalCodeStatus.Available)
            .GroupBy(c => c.VariantId)
            .Select(g => new { VariantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.VariantId, g => g.Count, cancellationToken);

        var result = new ExcelOrderImportPreviewResultDto();

        // 1. Đọc file Excel dạng dictionary từng dòng bằng MiniExcel
        var rows = fileStream.Query(useHeaderRow: true).ToList();
        if (rows.Count == 0)
        {
            return result;
        }

        // 2. Nhận diện tiêu đề cột thông minh (Fuzzy Column Mapping)
        var firstRow = rows[0] as IDictionary<string, object>;
        if (firstRow == null)
        {
            return result;
        }

        var headerMap = MapHeaders(firstRow.Keys);

        int rowIndex = 1; // 1-indexed (hàng 1 là header, dữ liệu từ hàng 2)
        var previewRows = new List<ExcelOrderPreviewRowDto>();

        foreach (var r in rows)
        {
            rowIndex++;
            var dict = r as IDictionary<string, object>;
            if (dict == null) continue;

            string rawOrderCode = GetValue(dict, headerMap, "order_no");
            string rawSkuOrProduct = GetValue(dict, headerMap, "sku");
            string rawQty = GetValue(dict, headerMap, "quantity");
            string rawPrice = GetValue(dict, headerMap, "price");
            string rawEmail = GetValue(dict, headerMap, "email");
            string rawPhone = GetValue(dict, headerMap, "phone");
            string rawName = GetValue(dict, headerMap, "name");
            string rawNote = GetValue(dict, headerMap, "note");

            // Bỏ qua dòng trống hoàn toàn
            if (string.IsNullOrWhiteSpace(rawSkuOrProduct) &&
                string.IsNullOrWhiteSpace(rawOrderCode) &&
                string.IsNullOrWhiteSpace(rawEmail))
            {
                continue;
            }

            int.TryParse(rawQty, out int qty);
            if (qty <= 0) qty = 1;

            long.TryParse(rawPrice, out long price);

            var previewRow = new ExcelOrderPreviewRowDto
            {
                RowIndex = rowIndex,
                RawOrderCode = rawOrderCode,
                RawSkuOrProduct = rawSkuOrProduct,
                Quantity = qty,
                UnitPrice = price,
                CustomerEmail = rawEmail,
                CustomerPhone = rawPhone,
                CustomerName = rawName,
                Note = rawNote,
                IsValid = true
            };

            // 3. Khớp SKU hoặc Tên sản phẩm (Fuzzy Matching)
            var matched = ResolveVariant(rawSkuOrProduct, variants);
            if (matched.Variant == null)
            {
                previewRow.IsValid = false;
                previewRow.ErrorMessage = $"Không tìm thấy sản phẩm/SKU '{rawSkuOrProduct}' trong gian hàng của bạn.";
                previewRow.MatchConfidence = 0;
            }
            else
            {
                previewRow.MatchedVariantId = matched.Variant.Id;
                previewRow.MatchedProductId = matched.Variant.ProductId;
                previewRow.MatchedProductName = matched.Variant.Product?.Name ?? string.Empty;
                previewRow.MatchedVariantLabel = !string.IsNullOrWhiteSpace(matched.Variant.Sku) ? matched.Variant.Sku : matched.Variant.CombinationKey;
                previewRow.MatchedSku = matched.Variant.Sku;
                previewRow.MatchConfidence = matched.Confidence;

                int availableStock = stockCounts.TryGetValue(matched.Variant.Id, out var s) ? s : 0;
                previewRow.AvailableStock = availableStock;

                if (previewRow.UnitPrice <= 0)
                {
                    previewRow.UnitPrice = matched.Variant.Price;
                }

                // Kiểm tra định dạng email nếu có cung cấp
                if (!string.IsNullOrWhiteSpace(previewRow.CustomerEmail) &&
                    !Regex.IsMatch(previewRow.CustomerEmail.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    previewRow.IsValid = false;
                    previewRow.ErrorMessage = $"Địa chỉ email '{previewRow.CustomerEmail}' không đúng định dạng.";
                }
            }

            previewRows.Add(previewRow);
        }

        // 4. Gom nhóm dòng thành từng Đơn hàng (Order Aggregation)
        var groupedOrders = GroupRowsIntoOrders(previewRows, stockCounts);

        result.AllRows = previewRows;
        result.TotalRows = previewRows.Count;
        result.ValidRows = previewRows.Count(r => r.IsValid);
        result.InvalidRows = previewRows.Count(r => !r.IsValid);
        result.Orders = groupedOrders;
        result.TotalOrderCount = groupedOrders.Count;
        result.ReadyToCreateOrderCount = groupedOrders.Count(o => o.CanBeCreated);

        return result;
    }

    public async Task<ExecuteExcelImportResultDto> ExecuteExcelImportAsync(
        Guid sellerUserId,
        ExecuteExcelImportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var shop = await _context.Shops
            .FirstOrDefaultAsync(s => s.OwnerUserId == sellerUserId, cancellationToken);

        if (shop == null)
        {
            throw new InvalidOperationException("Không tìm thấy gian hàng của người bán.");
        }

        var result = new ExecuteExcelImportResultDto();
        var ordersToCreate = request.SkipInvalidOrders
            ? request.Orders.Where(o => o.CanBeCreated).ToList()
            : request.Orders;

        if (!request.SkipInvalidOrders && request.Orders.Any(o => !o.CanBeCreated))
        {
            throw new InvalidOperationException("Có đơn hàng chứa dữ liệu không hợp lệ. Vui lòng khắc phục hoặc chọn chế độ 'Chỉ tạo các đơn hợp lệ'.");
        }

        if (ordersToCreate.Count == 0)
        {
            return result;
        }

        // Sử dụng Execution Strategy để hỗ trợ giao dịch an toàn
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (var ordGroup in ordersToCreate)
                {
                    // 1. Tạo phiên Checkout nội bộ cho đơn hàng ngoài
                    string checkoutNo = $"CK-EXCEL-{DateTime.UtcNow:yyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
                    var checkout = new Checkout(
                        checkoutNo,
                        sellerUserId,
                        ordGroup.TotalAmount,
                        0,
                        ordGroup.TotalAmount,
                        DateTime.UtcNow.AddMinutes(30)
                    );
                    checkout.MarkPaid();
                    await _context.Checkouts.AddAsync(checkout, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);

                    // 2. Tạo ShopOrder
                    string orderNo = !string.IsNullOrWhiteSpace(ordGroup.OrderNo)
                        ? ordGroup.OrderNo.Trim().ToUpper()
                        : $"OD-EX-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

                    // Đảm bảo không trùng OrderNo
                    if (await _context.ShopOrders.AnyAsync(o => o.OrderNo == orderNo, cancellationToken))
                    {
                        orderNo = $"OD-EX-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
                    }

                    string customerInfo = $"{ordGroup.CustomerName ?? ""} - {ordGroup.CustomerEmail ?? ""} - {ordGroup.CustomerPhone ?? ""}".Trim(' ', '-');
                    var shopOrder = new ShopOrder(
                        orderNo,
                        checkout.Id,
                        shop.Id,
                        sellerUserId,
                        ordGroup.TotalAmount,
                        ordGroup.TotalAmount,
                        0, // Đơn nhập ngoài không qua cổng sàn, không tính phí sàn mặc định
                        ordGroup.TotalAmount,
                        DateTime.UtcNow,
                        DateTime.UtcNow.AddDays(2)
                    );
                    shopOrder.SetSourceInfo(OrderSource.ExcelImport, customerInfo, ordGroup.Note);
                    await _context.ShopOrders.AddAsync(shopOrder, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);

                    // 3. Tạo Fulfillment và xuất kho mã số DigitalCodes
                    var fulfillment = new Fulfillment(shopOrder.Id, FulfillmentStatus.Fulfilled);
                    await _context.Fulfillments.AddAsync(fulfillment, cancellationToken);

                    foreach (var itemRow in ordGroup.Items)
                    {
                        if (!itemRow.MatchedVariantId.HasValue) continue;

                        var orderItem = new OrderItem(
                            shopOrder.Id,
                            itemRow.MatchedVariantId.Value,
                            itemRow.MatchedProductId ?? Guid.Empty,
                            null,
                            itemRow.MatchedProductName ?? "Sản phẩm",
                            itemRow.MatchedVariantLabel ?? "Biến thể",
                            itemRow.MatchedSku ?? "SKU",
                            itemRow.UnitPrice,
                            itemRow.Quantity,
                            itemRow.UnitPrice * itemRow.Quantity,
                            itemRow.UnitPrice * itemRow.Quantity,
                            0,
                            0,
                            itemRow.UnitPrice * itemRow.Quantity,
                            DeliveryType.CodePool
                        );
                        await _context.OrderItems.AddAsync(orderItem, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);

                        // Khóa và rút đúng số lượng mã Available
                        var availableCodes = await _context.DigitalCodes
                            .Where(c => c.ShopId == shop.Id && c.VariantId == itemRow.MatchedVariantId.Value && c.Status == DigitalCodeStatus.Available)
                            .OrderBy(c => c.CreatedAtUtc)
                            .Take(itemRow.Quantity)
                            .ToListAsync(cancellationToken);

                        if (availableCodes.Count < itemRow.Quantity)
                        {
                            throw new InvalidOperationException($"Biến thể '{itemRow.MatchedVariantLabel}' không còn đủ {itemRow.Quantity} mã trong kho tại thời điểm xuất đơn.");
                        }

                        foreach (var code in availableCodes)
                        {
                            code.MarkDelivered(orderItem.Id);
                        }

                        var fulfillmentItem = new FulfillmentItem(fulfillment.Id, orderItem.Id, itemRow.Quantity);
                        await _context.FulfillmentItems.AddAsync(fulfillmentItem, cancellationToken);
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                    result.CreatedOrderNos.Add(orderNo);
                    result.SuccessCount++;
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Lỗi khi thực thi nhập đơn Excel: {Message}", ex.Message);
                result.Errors.Add($"Giao dịch bị hoàn tác: {ex.Message}");
                result.FailedCount = ordersToCreate.Count;
                result.SuccessCount = 0;
                result.CreatedOrderNos.Clear();
            }
        });

        return result;
    }

    public async Task<byte[]> ExportErrorRowsAsync(
        Guid sellerUserId,
        List<ExcelOrderPreviewRowDto> errorRows,
        CancellationToken cancellationToken = default)
    {
        var exportData = errorRows.Select(r => new
        {
            Dòng = r.RowIndex,
            Mã_Đơn = r.RawOrderCode ?? "",
            Mã_SKU_Hoặc_Tên_SP = r.RawSkuOrProduct ?? "",
            Số_Lượng = r.Quantity,
            Đơn_Giá = r.UnitPrice,
            Email_Khách = r.CustomerEmail ?? "",
            Số_Điện_Thoại = r.CustomerPhone ?? "",
            Tên_Khách_Hàng = r.CustomerName ?? "",
            Ghi_Chú = r.Note ?? "",
            NGUYÊN_NHÂN_LỖI = r.ErrorMessage ?? "Không hợp lệ"
        }).ToList();

        using var memoryStream = new MemoryStream();
        await memoryStream.SaveAsAsync(exportData, cancellationToken: cancellationToken);
        return memoryStream.ToArray();
    }

    public async Task<byte[]> GenerateTemplateAsync(
        Guid sellerUserId,
        CancellationToken cancellationToken = default)
    {
        var shop = await _context.Shops
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.OwnerUserId == sellerUserId, cancellationToken);

        var variants = shop != null
            ? await _context.ProductVariants
                .AsNoTracking()
                .Include(v => v.Product)
                .Where(v => v.Product != null && v.Product.ShopId == shop.Id && v.Product.Status == ProductStatus.Listed)
                .Take(10)
                .ToListAsync(cancellationToken)
            : new List<ProductVariant>();

        var sampleData = new List<Dictionary<string, object>>
        {
            new()
            {
                ["Mã đơn"] = "ORD-001",
                ["Mã SKU"] = variants.Count > 0 ? (!string.IsNullOrWhiteSpace(variants[0].Sku) ? variants[0].Sku : variants[0].CombinationKey) : "KEY-WIN11-PRO",
                ["Số lượng"] = 1,
                ["Đơn giá"] = variants.Count > 0 ? variants[0].Price : 150000,
                ["Email khách"] = "khachhang1@gmail.com",
                ["Số điện thoại"] = "0987654321",
                ["Tên khách hàng"] = "Nguyễn Văn A",
                ["Ghi chú"] = "Khách chuyển khoản trực tiếp qua Vietcombank"
            },
            new()
            {
                ["Mã đơn"] = "ORD-001",
                ["Mã SKU"] = variants.Count > 1 ? (!string.IsNullOrWhiteSpace(variants[1].Sku) ? variants[1].Sku : variants[1].CombinationKey) : "KEY-OFFICE-365",
                ["Số lượng"] = 2,
                ["Đơn giá"] = variants.Count > 1 ? variants[1].Price : 250000,
                ["Email khách"] = "khachhang1@gmail.com",
                ["Số điện thoại"] = "0987654321",
                ["Tên khách hàng"] = "Nguyễn Văn A",
                ["Ghi chú"] = "Cùng 1 đơn mua 2 sản phẩm"
            },
            new()
            {
                ["Mã đơn"] = "ORD-002",
                ["Mã SKU"] = variants.Count > 0 ? (!string.IsNullOrWhiteSpace(variants[0].Sku) ? variants[0].Sku : variants[0].CombinationKey) : "KEY-WIN11-PRO",
                ["Số lượng"] = 1,
                ["Đơn giá"] = variants.Count > 0 ? variants[0].Price : 150000,
                ["Email khách"] = "khachhang2@gmail.com",
                ["Số điện thoại"] = "0912345678",
                ["Tên khách hàng"] = "Trần Thị B",
                ["Ghi chú"] = "Bán tại quầy"
            }
        };

        using var memoryStream = new MemoryStream();
        await memoryStream.SaveAsAsync(sampleData, cancellationToken: cancellationToken);
        return memoryStream.ToArray();
    }

    public async Task<SellerOrderDetailDto> CreateManualOrderAsync(
        Guid sellerUserId,
        CreateManualOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var shop = await _context.Shops
            .FirstOrDefaultAsync(s => s.OwnerUserId == sellerUserId, cancellationToken);

        if (shop == null)
        {
            throw new InvalidOperationException("Không tìm thấy gian hàng của người bán.");
        }

        var variant = await _context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == request.VariantId && v.Product != null && v.Product.ShopId == shop.Id, cancellationToken);

        if (variant == null)
        {
            throw new InvalidOperationException("Không tìm thấy biến thể sản phẩm thuộc gian hàng của bạn.");
        }

        if (request.Quantity <= 0)
        {
            throw new ArgumentException("Số lượng đặt phải lớn hơn 0.", nameof(request.Quantity));
        }

        long unitPrice = request.CustomUnitPrice.HasValue && request.CustomUnitPrice.Value > 0
            ? request.CustomUnitPrice.Value
            : variant.Price;

        long total = unitPrice * request.Quantity;

        SellerOrderDetailDto? resultDto = null;
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Kiểm tra tồn kho số khả dụng trong transaction
                var availableCodes = await _context.DigitalCodes
                    .Where(c => c.ShopId == shop.Id && c.VariantId == variant.Id && c.Status == DigitalCodeStatus.Available)
                    .OrderBy(c => c.CreatedAtUtc)
                    .Take(request.Quantity)
                    .ToListAsync(cancellationToken);

                if (availableCodes.Count < request.Quantity)
                {
                    throw new InvalidOperationException($"Kho số chỉ còn {availableCodes.Count} mã khả dụng, không đủ để tạo đơn số lượng {request.Quantity}.");
                }

                // Tạo phiên Checkout nội bộ
                string checkoutNo = $"CK-MANUAL-{DateTime.UtcNow:yyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
                var checkout = new Checkout(checkoutNo, sellerUserId, total, 0, total, DateTime.UtcNow.AddMinutes(30));
                checkout.MarkPaid();
                await _context.Checkouts.AddAsync(checkout, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                // Tạo ShopOrder
                string orderNo = $"OD-MN-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
                string customerInfo = $"{request.CustomerName ?? ""} - {request.CustomerEmail} - {request.CustomerPhone ?? ""}".Trim(' ', '-');

                var shopOrder = new ShopOrder(
                    orderNo,
                    checkout.Id,
                    shop.Id,
                    sellerUserId,
                    total,
                    total,
                    0,
                    total,
                    DateTime.UtcNow,
                    DateTime.UtcNow.AddDays(2)
                );
                shopOrder.SetSourceInfo(OrderSource.ManualEntry, customerInfo, request.Note);
                await _context.ShopOrders.AddAsync(shopOrder, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                // Tạo OrderItem
                var variantLabel = !string.IsNullOrWhiteSpace(variant.Sku) ? variant.Sku : variant.CombinationKey;
                var orderItem = new OrderItem(
                    shopOrder.Id,
                    variant.Id,
                    variant.ProductId,
                    null,
                    variant.Product?.Name ?? "Sản phẩm",
                    variantLabel,
                    variant.Sku ?? string.Empty,
                    unitPrice,
                    request.Quantity,
                    total,
                    total,
                    0,
                    0,
                    total,
                    DeliveryType.CodePool
                );
                await _context.OrderItems.AddAsync(orderItem, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                // Xuất giao mã số
                var fulfillment = new Fulfillment(shopOrder.Id, FulfillmentStatus.Fulfilled);
                await _context.Fulfillments.AddAsync(fulfillment, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                foreach (var code in availableCodes)
                {
                    code.MarkDelivered(orderItem.Id);
                }

                var fulfillmentItem = new FulfillmentItem(fulfillment.Id, orderItem.Id, request.Quantity);
                await _context.FulfillmentItems.AddAsync(fulfillmentItem, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                resultDto = new SellerOrderDetailDto
                {
                    Id = shopOrder.Id,
                    OrderNo = shopOrder.OrderNo,
                    BuyerUserId = sellerUserId,
                    Status = shopOrder.Status.ToString(),
                    Total = shopOrder.Total,
                    ShopGross = shopOrder.ShopGross,
                    CommissionTotal = shopOrder.CommissionTotal,
                    ShopNet = shopOrder.ShopNet,
                    DeliveredAtUtc = shopOrder.DeliveredAtUtc,
                    CreatedAtUtc = shopOrder.CreatedAtUtc,
                    Items = new List<SellerOrderItemDto>
                    {
                        new()
                        {
                            Id = orderItem.Id,
                            ProductId = orderItem.ProductId,
                            VariantId = orderItem.VariantId,
                            ProductName = orderItem.ProductName,
                            VariantLabel = orderItem.VariantLabel,
                            Sku = orderItem.Sku,
                            UnitPrice = orderItem.UnitPrice,
                            Quantity = orderItem.Quantity,
                            LineTotal = orderItem.LineTotal,
                            ShopNet = orderItem.ShopNet,
                            DeliveryType = orderItem.DeliveryType.ToString()
                        }
                    }
                };
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });

        return resultDto!;
    }

    #region Thuật toán Hỗ trợ (Fuzzy Mapping & Matching)

    private static Dictionary<string, string> MapHeaders(IEnumerable<string> headers)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var h in headers)
        {
            var normalized = NormalizeKey(h);
            if (normalized.Contains("madon") || normalized.Contains("order") || normalized.Contains("mado"))
                map["order_no"] = h;
            else if (normalized.Contains("sku") || normalized.Contains("masp") || normalized.Contains("bienthe") || normalized.Contains("sanpham"))
                map["sku"] = h;
            else if (normalized.Contains("soluong") || normalized.Contains("qty") || normalized.Contains("sl"))
                map["quantity"] = h;
            else if (normalized.Contains("dongia") || normalized.Contains("giaban") || normalized.Contains("gia") || normalized.Contains("price"))
                map["price"] = h;
            else if (normalized.Contains("email") || normalized.Contains("mail"))
                map["email"] = h;
            else if (normalized.Contains("sdt") || normalized.Contains("dienthoai") || normalized.Contains("phone"))
                map["phone"] = h;
            else if (normalized.Contains("ten") || normalized.Contains("hoten") || normalized.Contains("khachhang") || normalized.Contains("customer"))
                map["name"] = h;
            else if (normalized.Contains("ghichu") || normalized.Contains("note"))
                map["note"] = h;
        }

        return map;
    }

    private static string GetValue(IDictionary<string, object> dict, Dictionary<string, string> headerMap, string key)
    {
        if (headerMap.TryGetValue(key, out var headerName) && dict.TryGetValue(headerName, out var val) && val != null)
        {
            return val.ToString()?.Trim() ?? string.Empty;
        }
        return string.Empty;
    }

    private static (ProductVariant? Variant, double Confidence) ResolveVariant(string raw, List<ProductVariant> variants)
    {
        if (string.IsNullOrWhiteSpace(raw)) return (null, 0);

        string search = raw.Trim();

        // 1. Khớp chính xác SKU
        var exactSku = variants.FirstOrDefault(v => string.Equals(v.Sku?.Trim(), search, StringComparison.OrdinalIgnoreCase));
        if (exactSku != null) return (exactSku, 100.0);

        // 2. Khớp chính xác Tên biến thể (CombinationKey) hoặc Tên sản phẩm
        var exactName = variants.FirstOrDefault(v =>
            string.Equals(v.CombinationKey.Trim(), search, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(v.Product?.Name.Trim(), search, StringComparison.OrdinalIgnoreCase));
        if (exactName != null) return (exactName, 95.0);

        // 3. Khớp mờ theo chứa chuỗi con
        string searchNorm = NormalizeKey(search);
        var containsMatch = variants.FirstOrDefault(v =>
            NormalizeKey(v.Sku ?? "").Contains(searchNorm) ||
            NormalizeKey(v.CombinationKey).Contains(searchNorm) ||
            NormalizeKey(v.Product?.Name ?? "").Contains(searchNorm));
        if (containsMatch != null) return (containsMatch, 85.0);

        return (null, 0);
    }

    private static List<ExcelOrderGroupPreviewDto> GroupRowsIntoOrders(
        List<ExcelOrderPreviewRowDto> rows,
        Dictionary<Guid, int> stockCounts)
    {
        var groups = new List<ExcelOrderGroupPreviewDto>();
        // Tính lũy kế số lượng đặt theo variant để phát hiện quá tồn kho
        var allocatedStock = new Dictionary<Guid, int>();

        // Nhóm theo mã đơn nếu có, hoặc theo Email+SĐT
        var grouped = rows.GroupBy(r =>
        {
            if (!string.IsNullOrWhiteSpace(r.RawOrderCode))
                return $"ORDER:{r.RawOrderCode.Trim().ToUpper()}";
            if (!string.IsNullOrWhiteSpace(r.CustomerEmail))
                return $"EMAIL:{r.CustomerEmail.Trim().ToLower()}";
            return $"ROW:{r.RowIndex}";
        });

        foreach (var g in grouped)
        {
            var items = g.ToList();
            var first = items.First();

            var ord = new ExcelOrderGroupPreviewDto
            {
                OrderGroupKey = g.Key,
                OrderNo = first.RawOrderCode,
                CustomerEmail = first.CustomerEmail,
                CustomerPhone = first.CustomerPhone,
                CustomerName = first.CustomerName,
                Note = first.Note,
                Items = items,
                CanBeCreated = true
            };

            long total = 0;
            foreach (var item in items)
            {
                total += item.UnitPrice * item.Quantity;

                if (!item.IsValid)
                {
                    ord.CanBeCreated = false;
                    ord.BlockingErrors.Add($"Hàng {item.RowIndex}: {item.ErrorMessage}");
                }
                else if (item.MatchedVariantId.HasValue)
                {
                    var vid = item.MatchedVariantId.Value;
                    allocatedStock.TryGetValue(vid, out int currentUsed);
                    int remainingStock = item.AvailableStock - currentUsed;

                    if (item.Quantity > remainingStock)
                    {
                        item.IsValid = false;
                        item.ErrorMessage = $"Kho không đủ hàng (Cần {item.Quantity}, tồn kho khả dụng còn lại {Math.Max(0, remainingStock)}).";
                        ord.CanBeCreated = false;
                        ord.BlockingErrors.Add($"Hàng {item.RowIndex}: Kho không đủ hàng cho biến thể '{item.MatchedVariantLabel}'.");
                    }
                    else
                    {
                        allocatedStock[vid] = currentUsed + item.Quantity;
                    }
                }
            }

            ord.TotalAmount = total;
            groups.Add(ord);
        }

        return groups;
    }

    private static string NormalizeKey(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var lower = text.ToLowerInvariant();
        return Regex.Replace(lower, @"[^a-z0-9]", "");
    }

    #endregion
}
