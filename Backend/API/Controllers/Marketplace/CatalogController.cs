using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
[AllowAnonymous]
public class CatalogController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public CatalogController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    /// <summary>
    /// Duyệt danh sách sản phẩm công khai trên sàn thương mại số (hỗ trợ lọc danh mục, tìm kiếm, khoảng giá và sắp xếp)
    /// </summary>
    [HttpGet("api/products")]
    [ProducesResponseType(typeof(PagedResult<CatalogProductCardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts([FromQuery] CatalogFilterQuery query, CancellationToken cancellationToken)
    {
        var result = await _catalogService.GetCatalogProductsAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Xem chi tiết sản phẩm công khai theo slug (có bộ nhớ đệm Redis/Memory Cache)
    /// </summary>
    [HttpGet("api/products/{slug}")]
    [ProducesResponseType(typeof(CatalogProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await _catalogService.GetProductBySlugAsync(slug, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = $"Không tìm thấy sản phẩm '{slug}' hoặc sản phẩm hiện không công khai." });
        }
        return Ok(result);
    }

    /// <summary>
    /// Xem danh sách sản phẩm công khai của một gian hàng theo shop slug
    /// </summary>
    [HttpGet("api/shops/{slug}/products")]
    [ProducesResponseType(typeof(PagedResult<CatalogProductCardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShopPublicProducts(
        string slug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _catalogService.GetShopPublicProductsAsync(slug, page, pageSize, search, cancellationToken);
        return Ok(result);
    }
}
