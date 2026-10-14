using System.Text.Json;
using Application.Common;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace API.Filters;

/// <summary>
/// Attribute đánh dấu Action cần bảo vệ Idempotent (chống click đúp, retry trùng lặp)
/// Dựa trên header 'Idempotency-Key' và bảng IdempotencyRecords trong PostgreSQL.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var request = httpContext.Request;

        // Chỉ áp dụng cho các phương thức ghi (POST, PUT, PATCH, DELETE)
        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsOptions(request.Method))
        {
            await next();
            return;
        }

        // Lấy Idempotency-Key từ Header
        if (!request.Headers.TryGetValue("Idempotency-Key", out var rawKey) || string.IsNullOrWhiteSpace(rawKey))
        {
            // Nếu client không truyền header Idempotency-Key thì tiếp tục xử lý thông thường
            await next();
            return;
        }

        var key = rawKey.ToString().Trim();
        if (key.Length > 128)
        {
            context.Result = new BadRequestObjectResult(new { message = "Idempotency-Key không được vượt quá 128 ký tự." });
            return;
        }

        var dbContext = httpContext.RequestServices.GetRequiredService<SocialDbContext>();
        var currentUserService = httpContext.RequestServices.GetRequiredService<ICurrentUserService>();
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<IdempotentAttribute>>();

        var userId = currentUserService.UserId ?? Guid.Empty;
        var endpoint = request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // 1. Kiểm tra xem request này đã từng xử lý thành công trước đó chưa
        var existing = await dbContext.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == key && r.UserId == userId && r.Endpoint == endpoint, httpContext.RequestAborted);

        if (existing != null)
        {
            logger.LogInformation("Idempotent HIT: Yêu cầu với Key='{Key}', UserId='{UserId}', Endpoint='{Endpoint}' đã được phục vụ từ bản ghi lưu trữ.",
                key, userId, endpoint);

            httpContext.Response.Headers["X-Idempotent-Replay"] = "true";
            context.Result = new ContentResult
            {
                StatusCode = existing.StatusCode,
                Content = existing.ResponseBody,
                ContentType = "application/json"
            };
            return;
        }

        // 2. Chưa có bản ghi -> Thực thi action nghiệp vụ
        var executedContext = await next();

        // 3. Nếu thực thi thành công (Status 2xx), lưu lại snapshot phản hồi
        if (executedContext.Exception == null && executedContext.Result is ObjectResult objectResult)
        {
            var statusCode = objectResult.StatusCode ?? 200;
            if (statusCode >= 200 && statusCode < 300)
            {
                try
                {
                    var responseJson = JsonSerializer.Serialize(objectResult.Value, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    });

                    var record = new IdempotencyRecord
                    {
                        Key = key,
                        UserId = userId,
                        Endpoint = endpoint,
                        StatusCode = statusCode,
                        ResponseBody = responseJson
                    };

                    dbContext.IdempotencyRecords.Add(record);
                    await dbContext.SaveChangesAsync(CancellationToken.None);

                    logger.LogInformation("Idempotent STORED: Đã lưu bản ghi Idempotency cho Key='{Key}', Endpoint='{Endpoint}'.",
                        key, endpoint);
                }
                catch (DbUpdateException ex)
                {
                    // Trường hợp 2 request đồng thời cùng Key: Unique index ngăn chặn bản ghi thứ 2
                    logger.LogWarning(ex, "Xung đột ghi IdempotencyRecord đồng thời cho Key='{Key}'. Bỏ qua lưu đúp.", key);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Lỗi khi lưu IdempotencyRecord cho Key='{Key}'.", key);
                }
            }
        }
    }
}
