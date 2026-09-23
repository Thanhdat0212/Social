using Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace API.ErrorHandling;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Đã xảy ra lỗi khi xử lý request {Path}: {Message}", 
            httpContext.Request.Path, exception.Message);

        var (statusCode, title, detail, extensions) = MapException(exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        if (extensions != null)
        {
            foreach (var (key, value) in extensions)
            {
                problemDetails.Extensions[key] = value;
            }
        }

        // Chỉ hiển thị stack trace chi tiết ở môi trường Development
        if (_environment.IsDevelopment() && statusCode == StatusCodes.Status500InternalServerError)
        {
            problemDetails.Extensions["exception"] = exception.GetType().Name;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, string Detail, Dictionary<string, object?>? Extensions) MapException(Exception exception)
    {
        return exception switch
        {
            ValidationAppException validationEx => (
                validationEx.Errors.ContainsKey("EMAIL_NOT_CONFIRMED")
                    ? StatusCodes.Status403Forbidden
                    : StatusCodes.Status400BadRequest,
                validationEx.Errors.ContainsKey("EMAIL_NOT_CONFIRMED")
                    ? "Tài khoản chưa xác minh email"
                    : "Lỗi dữ liệu yêu cầu",
                validationEx.Message,
                validationEx.Errors.ContainsKey("EMAIL_NOT_CONFIRMED")
                    ? new Dictionary<string, object?> { ["code"] = "EMAIL_NOT_CONFIRMED" }
                    : (validationEx.Errors.Count > 0
                        ? new Dictionary<string, object?> { ["errors"] = validationEx.Errors }
                        : null)
            ),

            NotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                "Không tìm thấy tài nguyên",
                notFoundEx.Message,
                null
            ),

            ConflictException conflictEx => (
                StatusCodes.Status409Conflict,
                "Xung đột dữ liệu",
                conflictEx.Message,
                null
            ),

            UnauthorizedAccessException unauthorizedEx => (
                StatusCodes.Status401Unauthorized,
                "Không có quyền truy cập",
                unauthorizedEx.Message,
                null
            ),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Lỗi máy chủ nội bộ",
                "Đã xảy ra sự cố không mong muốn trên hệ thống. Vui lòng thử lại sau.",
                null
            )
        };
    }
}
