using API.Extensions;
using Application;
using Infrastructure;
using Infrastructure.Common;
using Infrastructure.External.Realtime;

// Nạp các biến môi trường từ file .env (nếu có) trước khi cấu hình ứng dụng
DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. Dependency Injection theo 3 tầng (Clean Architecture)
// ==========================================
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

// Tự động kiểm tra và áp dụng Database Migrations & nạp dữ liệu mẫu khởi động
await app.ApplyMigrationsAndSeedAsync();

// ==========================================
// 2. HTTP Request Pipeline (Thứ tự chuẩn mục 7.3)
// ==========================================

// 1. Forwarded Headers
app.UseForwardedHeaders();

// 2. Global Exception Handler
app.UseExceptionHandler();

// 3. Swagger (Development only)
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}

// 4. HTTPS Redirection
app.UseHttpsRedirection();

// 5. CORS
app.UseCors();

// 6. Rate Limiter (M9.3 Chống lạm dụng & Spam)
app.UseRateLimiter();

// 7. Authentication (Đọc JWT từ header hoặc query param cho WebSocket)
app.UseAuthentication();

// 8. Authorization (Kiểm tra [Authorize])
app.UseAuthorization();

// 9. Map Controllers & SignalR Hubs
app.MapControllers();
app.MapHub<SocialHub>("/hubs/social");

app.Run();
