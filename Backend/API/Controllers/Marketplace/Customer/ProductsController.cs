using Application.Common;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Products;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
[Route("api/shop/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IProductMediaStorageService _mediaStorageService;

    public ProductsController(
        IProductService productService,
        ICurrentUserService currentUserService,
        IProductMediaStorageService mediaStorageService)
    {
        _productService = productService;
        _currentUserService = currentUserService;
        _mediaStorageService = mediaStorageService;
    }

    private Guid CurrentUserId => _currentUserService.UserId ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

    /// <summary>
    /// Tạo bản nháp sản phẩm mới cho gian hàng
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateDraft([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var result = await _productService.CreateDraftAsync(CurrentUserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetProductById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Lấy danh sách sản phẩm trong gian hàng của tôi (phân trang, lọc theo trạng thái và tìm kiếm)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyProducts(
        [FromQuery] ProductStatus? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.GetShopProductsAsync(CurrentUserId, status, search, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy thông tin chi tiết một sản phẩm trong gian hàng
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _productService.GetProductByIdAsync(CurrentUserId, id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật thông tin cơ bản sản phẩm (Tên, danh mục, mô tả)
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateDetails(Guid id, [FromBody] UpdateProductDetailsRequest request, CancellationToken cancellationToken)
    {
        var result = await _productService.UpdateDetailsAsync(CurrentUserId, id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật giá bán nhanh cho một biến thể sản phẩm (Dành riêng cho Seller sở hữu shop)
    /// </summary>
    [HttpPatch("{id:guid}/variants/{variantId:guid}/price")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateVariantPrice(Guid id, Guid variantId, [FromBody] UpdateVariantPriceRequest request, CancellationToken cancellationToken)
    {
        var result = await _productService.UpdateVariantPriceAsync(CurrentUserId, id, variantId, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật các trục tùy chọn (Options) và ma trận biến thể (Variants)
    /// </summary>
    [HttpPut("{id:guid}/variants")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateVariants(Guid id, [FromBody] UpdateProductVariantsRequest request, CancellationToken cancellationToken)
    {
        var result = await _productService.UpdateVariantsAsync(CurrentUserId, id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật giá trị các thuộc tính động của sản phẩm
    /// </summary>
    [HttpPut("{id:guid}/attributes")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveAttributes(Guid id, [FromBody] SaveProductAttributeValuesRequest request, CancellationToken cancellationToken)
    {
        var result = await _productService.SaveAttributesAsync(CurrentUserId, id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Tải tệp hình ảnh sản phẩm từ máy tính lên Cloudinary
    /// </summary>
    [HttpPost("upload-image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductMediaUploadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadProductImage(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Tệp không hợp lệ",
                Detail = "Vui lòng chọn tệp ảnh để tải lên."
            });
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Tệp quá dung lượng cho phép",
                Detail = "Kích thước ảnh không được vượt quá 10MB."
            });
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Định dạng tệp không được hỗ trợ",
                Detail = "Vui lòng chọn ảnh định dạng JPG, PNG, WebP hoặc GIF."
            });
        }

        await using var stream = file.OpenReadStream();
        var (url, publicId) = await _mediaStorageService.UploadProductImageAsync(
            CurrentUserId,
            stream,
            file.FileName,
            cancellationToken);

        return Ok(new ProductMediaUploadResponseDto
        {
            Url = url,
            PublicId = publicId
        });
    }

    /// <summary>
    /// Thêm hình ảnh cho sản phẩm bằng URL đã có
    /// </summary>
    [HttpPost("{id:guid}/images")]
    [ProducesResponseType(typeof(ProductImageDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddImage(Guid id, [FromBody] UploadProductImageRequest request, CancellationToken cancellationToken)
    {
        var result = await _productService.AddImageAsync(CurrentUserId, id, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Tải tệp ảnh trực tiếp từ máy tính lên Cloudinary và gắn vào sản phẩm đã có
    /// </summary>
    [HttpPost("{id:guid}/images/upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductImageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadAndAddImage(
        Guid id,
        IFormFile file,
        [FromForm] bool isCover = false,
        [FromForm] int position = 0,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Tệp không hợp lệ",
                Detail = "Vui lòng chọn tệp ảnh để tải lên."
            });
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Tệp quá dung lượng cho phép",
                Detail = "Kích thước ảnh không được vượt quá 10MB."
            });
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Định dạng tệp không được hỗ trợ",
                Detail = "Vui lòng chọn ảnh định dạng JPG, PNG, WebP hoặc GIF."
            });
        }

        await using var stream = file.OpenReadStream();
        var (url, _) = await _mediaStorageService.UploadProductImageAsync(
            CurrentUserId,
            stream,
            file.FileName,
            cancellationToken);

        var result = await _productService.AddImageAsync(CurrentUserId, id, new UploadProductImageRequest
        {
            Url = url,
            IsCover = isCover,
            Position = position
        }, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Xóa hình ảnh của sản phẩm
    /// </summary>
    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteImage(Guid id, Guid imageId, CancellationToken cancellationToken)
    {
        await _productService.DeleteImageAsync(CurrentUserId, id, imageId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Sắp xếp thứ tự hình ảnh của sản phẩm
    /// </summary>
    [HttpPut("{id:guid}/images/reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReorderImages(Guid id, [FromBody] ReorderProductImagesRequest request, CancellationToken cancellationToken)
    {
        await _productService.ReorderImagesAsync(CurrentUserId, id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Đăng bán sản phẩm (Listed hoặc PendingReview nếu danh mục yêu cầu kiểm duyệt)
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        var result = await _productService.PublishAsync(CurrentUserId, id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Tạm ẩn sản phẩm khỏi sàn
    /// </summary>
    [HttpPost("{id:guid}/hide")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Hide(Guid id, CancellationToken cancellationToken)
    {
        var result = await _productService.HideAsync(CurrentUserId, id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Bỏ ẩn sản phẩm để hiển thị lại trên sàn
    /// </summary>
    [HttpPost("{id:guid}/unhide")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Unhide(Guid id, CancellationToken cancellationToken)
    {
        var result = await _productService.UnhideAsync(CurrentUserId, id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lưu trữ sản phẩm (ngừng kinh doanh hoàn toàn)
    /// </summary>
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _productService.ArchiveAsync(CurrentUserId, id, cancellationToken);
        return Ok(result);
    }
}
