using API.ErrorHandling;
using API.Extensions;
using Application;
using Infrastructure;
using Infrastructure.Common;
using Infrastructure.Persistence;
using Infrastructure.External.Realtime;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

// Nạp các biến môi trường từ file .env (nếu có) trước khi cấu hình ứng dụng
DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to container
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 2. Exception Handling & ProblemDetails
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 3. Forwarded Headers configuration (chuẩn bị cho 2 tầng proxy: Vercel -> Render)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// 4. CORS configuration & SignalR
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins);
        }
        else
        {
            policy.SetIsOriginAllowed(_ => true); // Cho phép kết nối dev / preview
        }
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddSignalR();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();


// 5. Rate Limiting (M9.3 Chống lạm dụng & Spam)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("global-policy", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 60;
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("checkout-policy", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 15;
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("report-policy", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 10;
        opt.QueueLimit = 0;
    });
});

// 6. Configure Swagger with JWT Authorize button
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Social API",
        Version = "v1",
        Description = "API mạng xã hội Social — Phase 1 MVP (Auth & Hồ sơ cá nhân)"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập trực tiếp Access Token (KHÔNG cần gõ chữ 'Bearer ', Swagger UI sẽ tự động gắn 'Bearer ' vào header)."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Tự động kiểm tra và áp dụng Database Migrations & nạp dữ liệu mẫu khởi động
await app.ApplyMigrationsAndSeedAsync();


// ==========================================
// HTTP Request Pipeline (Thứ tự chuẩn mục 7.3)
// ==========================================

// 1. Forwarded Headers
app.UseForwardedHeaders();

// 2. Global Exception Handler
app.UseExceptionHandler();

// 3. Swagger (Development only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Social API v1");
    });
}

// 4. HTTPS Redirection
app.UseHttpsRedirection();

// 5. CORS
app.UseCors();

// 6. Rate Limiter (M9.3)
app.UseRateLimiter();

// 7. Authentication (Đọc JWT từ header hoặc query param cho WebSocket)
app.UseAuthentication();

// 8. Authorization (Kiểm tra [Authorize])
app.UseAuthorization();

// 8. Map Controllers & SignalR Hubs
app.MapControllers();
app.MapHub<SocialHub>("/hubs/social");

// 9. Development & Testing Endpoints (Chỉ kích hoạt trong môi trường Development)
if (app.Environment.IsDevelopment())
{
    app.MapDevEndpoints();
}

app.Run();

