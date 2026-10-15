using Application.DTOs.Marketplace.Categories;
using Application.Interfaces.Marketplace.Categories;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Roles = "Admin")]
public class AdminCategoriesController : ControllerBase
{
    private readonly IAdminCategoryService _adminCategoryService;

    public AdminCategoriesController(IAdminCategoryService adminCategoryService)
    {
        _adminCategoryService = adminCategoryService;
    }

    /// <summary>
    /// Lấy toàn bộ cây danh mục cho Admin (kể cả danh mục đang tắt)
    /// </summary>
    [HttpGet("tree")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryTreeNodeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTree(CancellationToken cancellationToken)
    {
        var result = await _adminCategoryService.GetAllTreeAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Tạo mới một danh mục
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CategoryDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _adminCategoryService.CreateCategoryAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Cập nhật thông tin danh mục
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CategoryDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _adminCategoryService.UpdateCategoryAsync(id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Xóa một danh mục (chỉ khi không có danh mục con)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        await _adminCategoryService.DeleteCategoryAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Lấy danh sách toàn bộ thuộc tính sản phẩm
    /// </summary>
    [HttpGet("/api/admin/attributes")]
    [ProducesResponseType(typeof(IReadOnlyList<AttributeDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAttributes(CancellationToken cancellationToken)
    {
        var result = await _adminCategoryService.GetAllAttributesAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Tạo mới định nghĩa thuộc tính
    /// </summary>
    [HttpPost("/api/admin/attributes")]
    [ProducesResponseType(typeof(AttributeDefinitionDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAttribute([FromBody] CreateAttributeRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _adminCategoryService.CreateAttributeAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Cập nhật định nghĩa thuộc tính
    /// </summary>
    [HttpPut("/api/admin/attributes/{id:guid}")]
    [ProducesResponseType(typeof(AttributeDefinitionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAttribute(Guid id, [FromBody] UpdateAttributeRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _adminCategoryService.UpdateAttributeAsync(id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Thêm tùy chọn (Option) cho thuộc tính dạng Enum
    /// </summary>
    [HttpPost("/api/admin/attributes/{id:guid}/options")]
    [ProducesResponseType(typeof(AttributeOptionDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddOption(Guid id, [FromBody] CreateAttributeOptionDto request, CancellationToken cancellationToken)
    {
        var result = await _adminCategoryService.AddAttributeOptionAsync(id, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Xóa tùy chọn thuộc tính
    /// </summary>
    [HttpDelete("/api/admin/attributes/options/{optionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteOption(Guid optionId, CancellationToken cancellationToken)
    {
        await _adminCategoryService.DeleteAttributeOptionAsync(optionId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gán hoặc cập nhật cấu hình thuộc tính cho một danh mục
    /// </summary>
    [HttpPost("{id:guid}/attributes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignAttribute(Guid id, [FromBody] AssignCategoryAttributeRequestDto request, CancellationToken cancellationToken)
    {
        await _adminCategoryService.AssignAttributeToCategoryAsync(id, request, cancellationToken);
        return Ok(new { success = true, message = "Đã cấu hình thuộc tính cho danh mục." });
    }

    /// <summary>
    /// Gỡ bỏ thuộc tính khỏi danh mục
    /// </summary>
    [HttpDelete("{id:guid}/attributes/{attributeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveAttribute(Guid id, Guid attributeId, CancellationToken cancellationToken)
    {
        await _adminCategoryService.RemoveAttributeFromCategoryAsync(id, attributeId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Lấy danh sách yêu cầu mở danh mục từ Seller
    /// </summary>
    [HttpGet("/api/admin/category-requests")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRequests(
        [FromQuery] CategoryRequestStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _adminCategoryService.GetCategoryRequestsAsync(status, page, pageSize, cancellationToken);
        return Ok(new { items, total, page, pageSize });
    }

    /// <summary>
    /// Admin phê duyệt hoặc từ chối yêu cầu mở danh mục
    /// </summary>
    [HttpPost("/api/admin/category-requests/{id:guid}/review")]
    [ProducesResponseType(typeof(CategoryRequestDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReviewRequest(Guid id, [FromBody] ReviewCategoryRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _adminCategoryService.ReviewCategoryRequestAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
