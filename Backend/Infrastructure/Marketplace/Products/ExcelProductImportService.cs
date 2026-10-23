using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Application.Common;
using Application.DTOs.Marketplace.Inventory;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Inventory;
using Application.Interfaces.Marketplace.Products;
using Domain.Entities.Marketplace.Categories;
using Domain.Entities.Marketplace.Products;
using Domain.Entities.Marketplace.Shops;
using Domain.Enums;
using Infrastructure.External.Caching;
using Infrastructure.Marketplace.Shops;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;

namespace Infrastructure.Marketplace.Products;

public class ExcelProductImportService : IExcelProductImportService
{
    private readonly SocialDbContext _context;
    private readonly IInventoryService _inventoryService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ExcelProductImportService> _logger;

    public ExcelProductImportService(
        SocialDbContext context,
        IInventoryService inventoryService,
        ICacheService cacheService,
        ILogger<ExcelProductImportService> logger)
    {
        _context = context;
        _inventoryService = inventoryService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<ExcelProductImportPreviewResultDto> PreviewExcelProductsAsync(
        Guid sellerUserId,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        var shop = await _context.Shops
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.OwnerUserId == sellerUserId, cancellationToken);

        if (shop == null)
        {
            throw new InvalidOperationException("Không tìm thấy gian hàng của người bán. Vui lòng mở gian hàng trước khi nhập sản phẩm.");
        }

        // Tải danh sách SKU hiện có trong shop để đối chiếu trùng lặp
        var existingSkus = await _context.ProductVariants
            .AsNoTracking()
            .Where(v => v.ShopId == shop.Id && v.DeletedAtUtc == null)
            .Select(v => v.SkuNormalized)
            .ToListAsync(cancellationToken);
        var existingSkuSet = new HashSet<string>(existingSkus, StringComparer.OrdinalIgnoreCase);

        // Tải danh mục khả dụng
        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .ToListAsync(cancellationToken);

        var result = new ExcelProductImportPreviewResultDto();

        // 1. Đọc file Excel bằng MiniExcel (Streaming)
        var rows = fileStream.Query(useHeaderRow: true).ToList();
        if (rows.Count == 0)
        {
            return result;
        }

        // 2. Nhận diện tiêu đề cột thông minh (Fuzzy Header Mapping)
        var firstRow = rows[0] as IDictionary<string, object>;
        if (firstRow == null)
        {
            return result;
        }

        var headerMap = MapHeaders(firstRow.Keys);

        int rowIndex = 1; // 1-indexed (Hàng 1 là Header, dữ liệu từ hàng 2)
        var previewRows = new List<ExcelProductPreviewRowDto>();
        var seenBatchSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in rows)
        {
            rowIndex++;
            if (r is not IDictionary<string, object> dict) continue;

            string rawProductName = GetValue(dict, headerMap, "product_name");
            string rawCategoryName = GetValue(dict, headerMap, "category");
            string rawVariantName = GetValue(dict, headerMap, "variant_name");
            string rawSku = GetValue(dict, headerMap, "sku");
            string rawPrice = GetValue(dict, headerMap, "price");
            string rawCompareAtPrice = GetValue(dict, headerMap, "compare_at_price");
            string rawDescription = GetValue(dict, headerMap, "description");
            string rawImageUrl = GetValue(dict, headerMap, "image_url");
            string rawInitialCodes = GetValue(dict, headerMap, "initial_codes");
            string rawPublish = GetValue(dict, headerMap, "publish");

            // Bỏ qua dòng trống hoàn toàn
            if (string.IsNullOrWhiteSpace(rawProductName) &&
                string.IsNullOrWhiteSpace(rawPrice) &&
                string.IsNullOrWhiteSpace(rawSku) &&
                string.IsNullOrWhiteSpace(rawCategoryName))
            {
                continue;
            }

            var rowDto = new ExcelProductPreviewRowDto
            {
                RowIndex = rowIndex,
                RawProductName = rawProductName,
                RawCategoryName = rawCategoryName,
                RawVariantName = rawVariantName,
                RawSku = rawSku,
                RawPrice = rawPrice,
                RawCompareAtPrice = rawCompareAtPrice,
                RawDescription = rawDescription,
                RawImageUrl = rawImageUrl,
                RawInitialCodes = rawInitialCodes,
                RawPublish = rawPublish,
                IsValid = true
            };

            var errors = new List<string>();

            // 1. Kiểm tra Tên sản phẩm
            if (string.IsNullOrWhiteSpace(rawProductName))
            {
                errors.Add("Tên sản phẩm không được để trống.");
            }
            else if (rawProductName.Trim().Length > 200)
            {
                errors.Add("Tên sản phẩm tối đa 200 ký tự.");
            }
            else
            {
                rowDto.ProductName = rawProductName.Trim();
            }

            // 2. Khớp Danh mục thông minh
            if (!string.IsNullOrWhiteSpace(rawCategoryName))
            {
                var matchedCat = MatchCategory(rawCategoryName.Trim(), categories);
                if (matchedCat != null)
                {
                    rowDto.MatchedCategoryId = matchedCat.Id;
                    rowDto.MatchedCategoryName = matchedCat.Name;
                    rowDto.CategoryName = matchedCat.Name;
                }
                else
                {
                    rowDto.CategoryName = rawCategoryName.Trim();
                    // Nếu không khớp chính xác danh mục nào thì vẫn cho phép tạo sản phẩm nháp (CategoryId = null) hoặc cảnh báo
                    rowDto.MatchedCategoryName = $"{rawCategoryName.Trim()} (Chưa khớp danh mục hệ thống)";
                }
            }

            // 3. Tên gói / biến thể
            rowDto.VariantName = string.IsNullOrWhiteSpace(rawVariantName) ? "Gói Tiêu Chuẩn" : rawVariantName.Trim();

            // 4. Kiểm tra Giá bán
            if (string.IsNullOrWhiteSpace(rawPrice))
            {
                errors.Add("Giá bán sản phẩm không được để trống.");
            }
            else
            {
                long parsedPrice = ParseMoney(rawPrice);
                if (parsedPrice <= 0)
                {
                    errors.Add($"Giá bán '{rawPrice}' không hợp lệ (phải là số nguyên > 0).");
                }
                else
                {
                    rowDto.Price = parsedPrice;
                }
            }

            // 5. Kiểm tra Giá gốc so sánh (nếu có)
            if (!string.IsNullOrWhiteSpace(rawCompareAtPrice))
            {
                long parsedComparePrice = ParseMoney(rawCompareAtPrice);
                if (parsedComparePrice > 0)
                {
                    if (rowDto.Price > 0 && parsedComparePrice < rowDto.Price)
                    {
                        errors.Add($"Giá gốc ({parsedComparePrice:N0} đ) không được nhỏ hơn giá bán ({rowDto.Price:N0} đ).");
                    }
                    else
                    {
                        rowDto.CompareAtPrice = parsedComparePrice;
                    }
                }
            }

            // 6. Kiểm tra Mã SKU
            if (!string.IsNullOrWhiteSpace(rawSku))
            {
                var skuTrim = rawSku.Trim();
                var skuNorm = skuTrim.ToUpperInvariant();

                if (skuTrim.Length > 100)
                {
                    errors.Add("Mã SKU tối đa 100 ký tự.");
                }
                else if (seenBatchSkus.Contains(skuNorm))
                {
                    errors.Add($"Mã SKU '{skuTrim}' bị trùng lặp ngay trong tệp Excel tải lên.");
                }
                else if (existingSkuSet.Contains(skuNorm))
                {
                    errors.Add($"Mã SKU '{skuTrim}' đã tồn tại trong gian hàng của bạn.");
                }
                else
                {
                    seenBatchSkus.Add(skuNorm);
                    rowDto.Sku = skuTrim;
                }
            }
            else
            {
                // SKU để trống sẽ tự sinh tự động
                rowDto.Sku = null;
            }

            // 7. Mô tả và Ảnh
            rowDto.Description = string.IsNullOrWhiteSpace(rawDescription) ? null : rawDescription.Trim();
            if (!string.IsNullOrWhiteSpace(rawImageUrl))
            {
                var imgTrim = rawImageUrl.Trim();
                if (Uri.TryCreate(imgTrim, UriKind.Absolute, out var uriResult) &&
                    (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                {
                    rowDto.ImageUrl = imgTrim;
                }
                else
                {
                    errors.Add("Link ảnh đại diện không phải là đường dẫn URL hợp lệ (phải bắt đầu bằng http:// hoặc https://).");
                }
            }

            // 8. Kho mã khởi tạo
            if (!string.IsNullOrWhiteSpace(rawInitialCodes))
            {
                var codeList = rawInitialCodes
                    .Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct()
                    .ToList();
                rowDto.InitialCodes = codeList;
            }

            // 9. Đăng bán ngay
            if (!string.IsNullOrWhiteSpace(rawPublish))
            {
                var pTrim = rawPublish.Trim().ToLowerInvariant();
                rowDto.PublishImmediate = pTrim is "có" or "co" or "yes" or "true" or "1" or "đăng bán" or "dang ban";
            }

            if (errors.Count > 0)
            {
                rowDto.IsValid = false;
                rowDto.ErrorMessage = string.Join("; ", errors);
            }

            previewRows.Add(rowDto);
        }

        // 3. Gom nhóm theo Tên sản phẩm để hỗ trợ sản phẩm đa biến thể
        var groupedProducts = new List<ExcelProductGroupPreviewDto>();
        var grouped = previewRows
            .Where(r => !string.IsNullOrWhiteSpace(r.ProductName))
            .GroupBy(r => r.ProductName!.Trim(), StringComparer.OrdinalIgnoreCase);

        foreach (var g in grouped)
        {
            var firstItem = g.First();
            var groupDto = new ExcelProductGroupPreviewDto
            {
                GroupKey = g.Key,
                ProductName = g.Key,
                MatchedCategoryId = firstItem.MatchedCategoryId,
                MatchedCategoryName = firstItem.MatchedCategoryName,
                Description = firstItem.Description,
                ImageUrl = firstItem.ImageUrl,
                PublishImmediate = g.Any(x => x.PublishImmediate),
                Variants = g.ToList()
            };

            var groupErrors = new List<string>();
            var invalidItems = g.Where(x => !x.IsValid).ToList();
            if (invalidItems.Count > 0)
            {
                groupErrors.Add($"Có {invalidItems.Count} biến thể/dòng bị lỗi cấu hình.");
            }

            // Nếu muốn đăng bán ngay nhưng không có ảnh
            if (groupDto.PublishImmediate && string.IsNullOrWhiteSpace(groupDto.ImageUrl))
            {
                groupErrors.Add("Sản phẩm muốn Đăng bán ngay cần có Link ảnh đại diện.");
            }

            groupDto.BlockingErrors = groupErrors;
            groupDto.CanBeCreated = groupErrors.Count == 0;
            groupedProducts.Add(groupDto);
        }

        result.TotalRows = previewRows.Count;
        result.ValidRows = previewRows.Count(r => r.IsValid);
        result.InvalidRows = previewRows.Count(r => !r.IsValid);
        result.TotalProductCount = groupedProducts.Count;
        result.ReadyToCreateProductCount = groupedProducts.Count(g => g.CanBeCreated);
        result.Products = groupedProducts;
        result.AllRows = previewRows;

        return result;
    }

    public async Task<ExecuteProductImportResultDto> ExecuteProductImportAsync(
        Guid sellerUserId,
        ExecuteProductImportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var shop = await _context.Shops
            .FirstOrDefaultAsync(s => s.OwnerUserId == sellerUserId, cancellationToken);

        if (shop == null)
        {
            throw new InvalidOperationException("Không tìm thấy gian hàng của người bán.");
        }

        var result = new ExecuteProductImportResultDto();

        if (request.Products == null || request.Products.Count == 0)
        {
            return result;
        }

        // Lọc danh sách sản phẩm cần tạo
        var targetProducts = request.SkipInvalidProducts
            ? request.Products.Where(p => p.CanBeCreated && p.Variants.All(v => v.IsValid)).ToList()
            : request.Products;

        if (!request.SkipInvalidProducts && request.Products.Any(p => !p.CanBeCreated || p.Variants.Any(v => !v.IsValid)))
        {
            result.FailedCount = request.Products.Count;
            result.Errors.Add("Quá trình nhập bị hủy bỏ do có sản phẩm chứa lỗi dữ liệu.");
            return result;
        }

        foreach (var prodInput in targetProducts)
        {
            try
            {
                // 1. Sinh Slug duy nhất cho sản phẩm
                var slug = await GenerateUniqueProductSlugAsync(prodInput.ProductName, cancellationToken);
                var product = new Product(shop.Id, prodInput.ProductName, slug, prodInput.MatchedCategoryId, prodInput.Description);

                var createdVariants = new List<(ProductVariant Variant, List<string> InitialCodes)>();

                if (prodInput.Variants.Count == 1)
                {
                    // Sản phẩm đơn biến thể
                    var vInput = prodInput.Variants[0];
                    var sku = string.IsNullOrWhiteSpace(vInput.Sku)
                        ? $"P{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}"
                        : vInput.Sku.Trim();

                    var variant = new ProductVariant(
                        product.Id,
                        shop.Id,
                        sku,
                        "default",
                        vInput.Price,
                        vInput.CompareAtPrice,
                        DeliveryType.CodePool,
                        trackInventory: true);

                    product.Variants.Add(variant);
                    product.UpdatePriceCache(variant.Price, variant.Price);
                    createdVariants.Add((variant, vInput.InitialCodes));
                }
                else
                {
                    // Sản phẩm có nhiều biến thể (Multi-Variants)
                    var optionName = "Gói sản phẩm";
                    var option = new ProductOption(product.Id, optionName, 0);
                    product.Options.Add(option);

                    int valPos = 0;
                    var optionValuesMap = new Dictionary<string, ProductOptionValue>(StringComparer.OrdinalIgnoreCase);
                    var resolvedVariantNames = new List<string>(prodInput.Variants.Count);

                    for (int idx = 0; idx < prodInput.Variants.Count; idx++)
                    {
                        var vInput = prodInput.Variants[idx];
                        var valName = string.IsNullOrWhiteSpace(vInput.VariantName) ? $"Gói {idx + 1}" : vInput.VariantName.Trim();
                        resolvedVariantNames.Add(valName);

                        if (!optionValuesMap.ContainsKey(valName))
                        {
                            var optVal = new ProductOptionValue(option.Id, valName, valPos++);
                            option.Values.Add(optVal);
                            optionValuesMap[valName] = optVal;
                        }
                    }

                    long minPrice = long.MaxValue;
                    long maxPrice = 0;

                    for (int idx = 0; idx < prodInput.Variants.Count; idx++)
                    {
                        var vInput = prodInput.Variants[idx];
                        var valName = resolvedVariantNames[idx];
                        var optVal = optionValuesMap[valName];

                        var sku = string.IsNullOrWhiteSpace(vInput.Sku)
                            ? $"P{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}"
                            : vInput.Sku.Trim();

                        var variant = new ProductVariant(
                            product.Id,
                            shop.Id,
                            sku,
                            valName,
                            vInput.Price,
                            vInput.CompareAtPrice,
                            DeliveryType.CodePool,
                            trackInventory: true);

                        variant.OptionValues.Add(new VariantOptionValue(variant.Id, optVal.Id));
                        product.Variants.Add(variant);

                        minPrice = Math.Min(minPrice, variant.Price);
                        maxPrice = Math.Max(maxPrice, variant.Price);
                        createdVariants.Add((variant, vInput.InitialCodes));
                    }

                    product.UpdatePriceCache(minPrice, maxPrice);
                }

                // 2. Thêm ảnh bìa nếu có URL hợp lệ
                if (!string.IsNullOrWhiteSpace(prodInput.ImageUrl))
                {
                    var image = new ProductImage(product.Id, prodInput.ImageUrl.Trim(), 0, isCover: true);
                    product.Images.Add(image);
                }

                // 3. Đăng bán ngay nếu được yêu cầu (tuân thủ chính sách kiểm duyệt danh mục)
                if (prodInput.PublishImmediate && !string.IsNullOrWhiteSpace(prodInput.ImageUrl))
                {
                    bool requiresReview = false;
                    if (prodInput.MatchedCategoryId.HasValue)
                    {
                        var cat = await _context.Categories.AsNoTracking()
                            .FirstOrDefaultAsync(c => c.Id == prodInput.MatchedCategoryId.Value, cancellationToken);
                        if (cat != null && cat.ModerationPolicy == CategoryModerationPolicy.RequiresReview)
                        {
                            requiresReview = true;
                        }
                    }
                    product.Publish(requiresReview);
                }

                // Lưu vào CSDL
                await _context.Products.AddAsync(product, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                // 4. Nạp kho số khởi tạo (nếu có mã)
                foreach (var (v, codes) in createdVariants)
                {
                    if (codes != null && codes.Count > 0)
                    {
                        try
                        {
                            await _inventoryService.BulkImportCodesAsync(
                                sellerUserId,
                                v.Id,
                                new BulkImportCodesRequest { Codes = codes },
                                cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Lỗi khi nạp mã kho số khởi tạo cho variant {VariantId}: {Message}", v.Id, ex.Message);
                        }
                    }
                }

                result.SuccessCount++;
                result.CreatedProductNames.Add(product.Name);
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add($"Lỗi khi tạo sản phẩm '{prodInput.ProductName}': {ex.Message}");
                _logger.LogError(ex, "Lỗi khi nhập sản phẩm từ Excel: {ProductName}", prodInput.ProductName);
            }
        }

        // Xóa cache gian hàng sau khi thêm mới thành công
        if (result.SuccessCount > 0)
        {
            _cacheService.Remove(CacheKeys.ShopPublic(shop.Slug));
        }

        return result;
    }

    public async Task<byte[]> GenerateTemplateAsync(
        Guid sellerUserId,
        CancellationToken cancellationToken = default)
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Take(100)
            .ToListAsync(cancellationToken);

        var sampleData = new List<Dictionary<string, object>>
        {
            new()
            {
                ["Tên sản phẩm"] = "Tài khoản Canva Pro Chính Chủ",
                ["Danh mục"] = "Tài khoản Premium",
                ["Tên gói"] = "Gói 1 Năm",
                ["Mã SKU"] = "CANVA-PRO-1Y",
                ["Giá bán"] = 150000,
                ["Giá gốc"] = 300000,
                ["Mô tả"] = "Nâng cấp chính chủ tài khoản Canva Pro đầy đủ tính năng AI, xuất file chất lượng cao.",
                ["Link ảnh"] = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe",
                ["Kho mã khởi tạo"] = "KEY-CANVA-001\nKEY-CANVA-002",
                ["Đăng bán ngay"] = "Có"
            },
            new()
            {
                ["Tên sản phẩm"] = "Key Windows 11 Pro Bản Quyền",
                ["Danh mục"] = "Phần mềm & Bản quyền",
                ["Tên gói"] = "Bản quyền trọn đời 1 PC",
                ["Mã SKU"] = "WIN11-PRO-OEM",
                ["Giá bán"] = 99000,
                ["Giá gốc"] = 250000,
                ["Mô tả"] = "Kích hoạt online trực tiếp, bảo hành vĩnh viễn theo máy.",
                ["Link ảnh"] = "https://images.unsplash.com/photo-1593642632823-8f785ba67e45",
                ["Kho mã khởi tạo"] = "W269N-WFGWX-YVC9B-4J6C9-T83GX",
                ["Đăng bán ngay"] = "Có"
            },
            new()
            {
                ["Tên sản phẩm"] = "Gói Netflix Premium 4K",
                ["Danh mục"] = "Tài khoản Premium",
                ["Tên gói"] = "1 Tháng (1 Profile)",
                ["Mã SKU"] = "NFLX-4K-1M",
                ["Giá bán"] = 65000,
                ["Giá gốc"] = 90000,
                ["Mô tả"] = "Xem phim chuẩn 4K UltraHD, profile riêng có mã PIN bảo mật.",
                ["Link ảnh"] = "https://images.unsplash.com/photo-1574375927938-d5a98e8ffe85",
                ["Kho mã khởi tạo"] = "",
                ["Đăng bán ngay"] = "Không"
            }
        };

        using var memoryStream = new MemoryStream();
        await memoryStream.SaveAsAsync(sampleData, cancellationToken: cancellationToken);
        return memoryStream.ToArray();
    }

    public async Task<byte[]> ExportErrorRowsAsync(
        Guid sellerUserId,
        List<ExcelProductPreviewRowDto> errorRows,
        CancellationToken cancellationToken = default)
    {
        var exportData = errorRows.Select(r => new Dictionary<string, object>
        {
            ["Dòng Excel"] = r.RowIndex,
            ["Tên sản phẩm"] = r.RawProductName ?? "",
            ["Danh mục"] = r.RawCategoryName ?? "",
            ["Tên gói"] = r.RawVariantName ?? "",
            ["Mã SKU"] = r.RawSku ?? "",
            ["Giá bán"] = r.RawPrice ?? "",
            ["Giá gốc"] = r.RawCompareAtPrice ?? "",
            ["Mô tả"] = r.RawDescription ?? "",
            ["Link ảnh"] = r.RawImageUrl ?? "",
            ["Kho mã khởi tạo"] = r.RawInitialCodes ?? "",
            ["Đăng bán ngay"] = r.RawPublish ?? "",
            ["Nguyên nhân lỗi vi sai"] = r.ErrorMessage ?? "Lỗi dữ liệu không xác định"
        }).ToList();

        using var memoryStream = new MemoryStream();
        await memoryStream.SaveAsAsync(exportData, cancellationToken: cancellationToken);
        return memoryStream.ToArray();
    }

    #region Helper Methods

    private async Task<string> GenerateUniqueProductSlugAsync(string name, CancellationToken cancellationToken)
    {
        var baseSlug = SlugHelper.GenerateSlug(name);
        var slug = baseSlug;
        var counter = 1;

        while (await _context.Products.AnyAsync(p => p.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        return slug;
    }

    private static Category? MatchCategory(string rawCatName, List<Category> categories)
    {
        if (string.IsNullOrWhiteSpace(rawCatName)) return null;

        var rawTrim = rawCatName.Trim();
        var rawNorm = RemoveDiacritics(rawTrim).ToLowerInvariant();

        // 1. Khớp chính xác tên
        var matched = categories.FirstOrDefault(c => string.Equals(c.Name.Trim(), rawTrim, StringComparison.OrdinalIgnoreCase));
        if (matched != null) return matched;

        // 2. Khớp Slug
        matched = categories.FirstOrDefault(c => string.Equals(c.Slug, rawNorm, StringComparison.OrdinalIgnoreCase));
        if (matched != null) return matched;

        // 3. Khớp bỏ dấu tiếng Việt
        matched = categories.FirstOrDefault(c => RemoveDiacritics(c.Name).Equals(rawNorm, StringComparison.OrdinalIgnoreCase));
        if (matched != null) return matched;

        // 4. Khớp chứa từ khóa
        matched = categories.FirstOrDefault(c => RemoveDiacritics(c.Name).Contains(rawNorm, StringComparison.OrdinalIgnoreCase) ||
                                                 rawNorm.Contains(RemoveDiacritics(c.Name), StringComparison.OrdinalIgnoreCase));
        return matched;
    }

    private static long ParseMoney(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0;

        // Loại bỏ ký tự tiền tệ, dấu cách, chữ đ, VND, vnđ
        var clean = Regex.Replace(raw, @"[^\d]", "");
        return long.TryParse(clean, out var val) ? val : 0;
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'D');
    }

    private static Dictionary<string, string> MapHeaders(IEnumerable<string> rawHeaders)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in rawHeaders)
        {
            if (string.IsNullOrWhiteSpace(header)) continue;
            var norm = RemoveDiacritics(header).ToLowerInvariant().Trim();

            if (norm.Contains("ten san pham") || norm.Contains("ten sp") || norm.Contains("product name") || (norm.Contains("ten") && !norm.Contains("goi") && !norm.Contains("khach")))
                map["product_name"] = header;
            else if (norm.Contains("danh muc") || norm.Contains("category") || norm.Contains("chuyen muc") || norm.Contains("nhom"))
                map["category"] = header;
            else if (norm.Contains("ten goi") || norm.Contains("goi") || norm.Contains("bien the") || norm.Contains("variant") || norm.Contains("loai"))
                map["variant_name"] = header;
            else if (norm.Contains("sku") || norm.Contains("ma sp") || norm.Contains("ma san pham"))
                map["sku"] = header;
            else if (norm.Contains("gia goc") || norm.Contains("gia niem yet") || norm.Contains("compare"))
                map["compare_at_price"] = header;
            else if (norm.Contains("gia ban") || norm.Contains("don gia") || norm.Contains("price") || norm.Contains("gia"))
                map["price"] = header;
            else if (norm.Contains("mo ta") || norm.Contains("description") || norm.Contains("chi tiet"))
                map["description"] = header;
            else if (norm.Contains("link anh") || norm.Contains("hinh anh") || norm.Contains("anh bia") || norm.Contains("image") || norm.Contains("anh"))
                map["image_url"] = header;
            else if (norm.Contains("kho ma") || norm.Contains("ma khoi tao") || norm.Contains("initial") || norm.Contains("code"))
                map["initial_codes"] = header;
            else if (norm.Contains("dang ban") || norm.Contains("xuat ban") || norm.Contains("publish") || norm.Contains("trang thai"))
                map["publish"] = header;
        }

        return map;
    }

    private static string GetValue(IDictionary<string, object> dict, Dictionary<string, string> headerMap, string key)
    {
        if (headerMap.TryGetValue(key, out var actualHeader) && dict.TryGetValue(actualHeader, out var val) && val != null)
        {
            return val.ToString()?.Trim() ?? string.Empty;
        }
        return string.Empty;
    }

    #endregion
}
