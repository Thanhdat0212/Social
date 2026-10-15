using Application.DTOs.Interests;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InterestsController : ControllerBase
{
    private readonly IInterestService _interestService;

    public InterestsController(IInterestService interestService)
    {
        _interestService = interestService;
    }

    /// <summary>
    /// Lấy danh sách tất cả các chủ đề/sở thích đang hoạt động trong hệ thống
    /// (Nếu đã đăng nhập, trường IsSelected sẽ phản ánh sở thích của người dùng)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<InterestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllInterests(CancellationToken cancellationToken)
    {
        var interests = await _interestService.GetAllInterestsAsync(cancellationToken);
        return Ok(interests);
    }
}
