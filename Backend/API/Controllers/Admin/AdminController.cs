using Application.DTOs.Auth.Responses;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly SocialDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(SocialDbContext context, ILogger<AdminController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Kiểm tra quyền Admin (Ping API để test Authorize)
    /// </summary>
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new
        {
            message = "Xác thực quyền Quản trị viên (Admin) thành công!",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Lấy danh sách người dùng trong hệ thống (có phân trang và lọc)
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] UserRole? role = null,
        [FromQuery] UserStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(u => EF.Functions.ILike(u.Email, $"%{s}%") || EF.Functions.ILike(u.DisplayName, $"%{s}%"));
        }

        if (role.HasValue)
        {
            query = query.Where(u => u.Role == role.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(u => u.Status == status.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.DisplayName,
                u.AvatarUrl,
                u.EmailConfirmed,
                Role = u.Role.ToString(),
                Status = u.Status.ToString(),
                u.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            total,
            page,
            pageSize,
            items = users
        });
    }

    /// <summary>
    /// Cập nhật trạng thái người dùng (Khóa / Mở khóa tài khoản)
    /// </summary>
    [HttpPatch("users/{id:guid}/status")]
    public async Task<IActionResult> UpdateUserStatus(
        Guid id,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentAdminIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst("sub")?.Value;
        if (Guid.TryParse(currentAdminIdStr, out var currentAdminId) && currentAdminId == id && request.Status == UserStatus.Suspended)
        {
            throw new ValidationAppException("Status", "Quản trị viên không thể tự khóa tài khoản của chính mình.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User", id);
        }

        var nowUtc = DateTime.UtcNow;
        user.Status = request.Status;
        user.UpdatedAtUtc = nowUtc;

        // Nếu khóa tài khoản, thu hồi toàn bộ RefreshToken đang hoạt động của người dùng đó
        if (request.Status == UserStatus.Suspended)
        {
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == id && rt.RevokedAtUtc == null && rt.ExpiresAtUtc > nowUtc)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
            {
                token.RevokedAtUtc = nowUtc;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin đã đổi trạng thái tài khoản {UserId} sang {Status}", id, request.Status);

        return Ok(new
        {
            message = $"Đã cập nhật trạng thái tài khoản sang {request.Status}",
            userId = user.Id,
            status = user.Status.ToString()
        });
    }

    /// <summary>
    /// Phân quyền người dùng (Admin / Member)
    /// </summary>
    [HttpPatch("users/{id:guid}/role")]
    public async Task<IActionResult> UpdateUserRole(
        Guid id,
        [FromBody] UpdateUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentAdminIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst("sub")?.Value;
        if (Guid.TryParse(currentAdminIdStr, out var currentAdminId) && currentAdminId == id && request.Role != UserRole.Admin)
        {
            throw new ValidationAppException("Role", "Quản trị viên không thể tự hạ quyền của chính mình.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User", id);
        }

        user.Role = request.Role;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin đã đổi vai trò tài khoản {UserId} sang {Role}", id, request.Role);

        return Ok(new
        {
            message = $"Đã cập nhật vai trò tài khoản sang {request.Role}",
            userId = user.Id,
            role = user.Role.ToString()
        });
    }
}

public class UpdateUserStatusRequest
{
    public UserStatus Status { get; set; }
}

public class UpdateUserRoleRequest
{
    public UserRole Role { get; set; }
}
