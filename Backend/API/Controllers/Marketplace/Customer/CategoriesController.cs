using Application.DTOs.Marketplace.Categories;
using Application.Interfaces.Marketplace.Categories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ISellerCategoryService _sellerCategoryService;

    public CategoriesController(
        ICategoryService categoryService,
        ISellerCategoryService sellerCategoryService)
    {
        _categoryService = categoryService;
        _sellerCategoryService = sellerCategoryService;
    }

    /// <summary>
    /// Lấy cây danh mục sản phẩm đang hoạt động (Đọc từ Memory Cache)
    /// </summary>
    [HttpGet("api/categories/tree")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryTreeNodeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTree(CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetTreeAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết danh mục
    /// </summary>
    [HttpGet("api/categories/{id:guid}")]
    [ProducesResponseType(typeof(CategoryDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetByIdAsync(id, cancellationToken);
        if (result == null) return NotFound(new { message = "Danh mục không tồn tại." });
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách thuộc tính của danh mục (kèm thuộc tính kế thừa từ tổ tiên, có Memory Cache)
    /// </summary>
    [HttpGet("api/categories/{id:guid}/attributes")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryAttributeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttributes(Guid id, CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetAttributesByCategoryIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Chủ gian hàng gửi yêu cầu mở thêm danh mục mới
    /// </summary>
    [HttpPost("api/category-requests")]
    [Authorize]
    [ProducesResponseType(typeof(CategoryRequestDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> RequestCategory([FromBody] CreateCategoryRequestFromSellerDto request, CancellationToken cancellationToken)
    {
        var result = await _sellerCategoryService.RequestCategoryAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Lấy danh sách đề xuất mở danh mục của gian hàng hiện tại
    /// </summary>
    [HttpGet("api/category-requests/my")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyRequests(CancellationToken cancellationToken)
    {
        var result = await _sellerCategoryService.GetMyCategoryRequestsAsync(cancellationToken);
        return Ok(result);
    }
}
