using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Realtime;

public class SocialHub : Hub
{
    // Khởi tạo & logger: 
    private readonly ILogger<SocialHub> _logger;// theo dõi ai vào, ai ra khỏi tổng đài của mình

    public SocialHub(ILogger<SocialHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId(); // Kiểm tra danh tính client qua token
        if (userId.HasValue)
        {
            // Tự động gia nhập group cá nhân của user để nhận thông báo
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
            _logger.LogInformation("Client {ConnectionId} của User {UserId} đã kết nối SocialHub.", Context.ConnectionId, userId.Value);
        }
        else
        {
            _logger.LogInformation("Khách vãng lai (Guest) {ConnectionId} đã kết nối SocialHub.", Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
            _logger.LogInformation("Client {ConnectionId} của User {UserId} đã ngắt kết nối SocialHub.", Context.ConnectionId, userId.Value);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Cho phép client tham gia group của một bài viết cụ thể để theo dõi realtime comments và likes
    /// </summary>
    public async Task JoinPostGroup(string postId)
    {
        if (Guid.TryParse(postId, out var parsedPostId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"post_{parsedPostId}");
            _logger.LogDebug("Client {ConnectionId} đã tham gia group post_{PostId}", Context.ConnectionId, parsedPostId);
        }
    }

    /// <summary>
    /// Rời khỏi group của một bài viết khi đóng dialog / chuyển trang
    /// </summary>
    public async Task LeavePostGroup(string postId)
    {
        if (Guid.TryParse(postId, out var parsedPostId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"post_{parsedPostId}");
            _logger.LogDebug("Client {ConnectionId} đã rời group post_{PostId}", Context.ConnectionId, parsedPostId);
        }
    }

    private Guid? GetUserId()
    {
        var claim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");

        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
