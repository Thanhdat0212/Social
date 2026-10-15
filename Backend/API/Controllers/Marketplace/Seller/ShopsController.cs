using Application.DTOs.Marketplace.Shops;
using Application.Interfaces.Marketplace.Shops;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
public class ShopsController : ControllerBase
{
    private readonly IShopService _shopService;

    public ShopsController(IShopService shopService)
    {
        _shopService = shopService;
    }

    /// <summary>
    /// Đăng ký mở gian hàng mới
    /// </summary>
    [HttpPost("api/shops")]
    [HttpPost("api/shop")]
    [Authorize]
    [ProducesResponseType(typeof(ShopDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateShop([FromBody] CreateShopRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _shopService.CreateShopAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetMyShop), new { }, result);
    }

    /// <summary>
    /// Lấy thông tin gian hàng của người dùng hiện tại (kèm trạng thái hiệu lực và lý do từ chối/khóa)
    /// </summary>
    [HttpGet("api/shop")]
    [Authorize]
    [ProducesResponseType(typeof(ShopDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyShop(CancellationToken cancellationToken)
    {
        var result = await _shopService.GetMyShopAsync(cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = "Bạn chưa có gian hàng nào." });
        }
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật thông tin gian hàng của người dùng hiện tại
    /// </summary>
    [HttpPut("api/shop")]
    [Authorize]
    [ProducesResponseType(typeof(ShopDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMyShop([FromBody] UpdateShopRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _shopService.UpdateMyShopAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Nộp lại hồ sơ xét duyệt sau khi bị từ chối (Cooldown 24h, tối đa 3 lần)
    /// </summary>
    [HttpPost("api/shop/resubmit")]
    [Authorize]
    [ProducesResponseType(typeof(ShopDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResubmitShop([FromBody] ResubmitShopRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _shopService.ResubmitShopAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Mở cửa gian hàng
    /// </summary>
    [HttpPost("api/shop/open")]
    [Authorize]
    [ProducesResponseType(typeof(ShopDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> OpenShop(CancellationToken cancellationToken)
    {
        var result = await _shopService.OpenMyShopAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Tạm đóng cửa gian hàng
    /// </summary>
    [HttpPost("api/shop/close")]
    [Authorize]
    [ProducesResponseType(typeof(ShopDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CloseShop(CancellationToken cancellationToken)
    {
        var result = await _shopService.CloseMyShopAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Xem thông tin gian hàng công khai theo slug (Có Memory Cache)
    /// </summary>
    [HttpGet("api/shops/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ShopSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicShop(string slug, CancellationToken cancellationToken)
    {
        var result = await _shopService.GetPublicShopBySlugAsync(slug, cancellationToken);
        return Ok(result);
    }
}
