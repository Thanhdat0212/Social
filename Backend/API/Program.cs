using API.ErrorHandling;
using Application;
using Infrastructure;
using Infrastructure.Common;
using Infrastructure.Persistence;
using Infrastructure.Realtime;
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

// Tự động kiểm tra và áp dụng Database Migrations khi khởi động
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SocialDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Đang kiểm tra và áp dụng Database Migrations...");

        // Tự động đồng bộ lịch sử migration cho PostEmbeddings nếu bảng đã được tạo thủ công trước đó
        try
        {
            await context.Database.ExecuteSqlRawAsync(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'PostEmbeddings') THEN
                        INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                        VALUES ('20260929044949_AddPostEmbeddings', '8.0.8')
                        ON CONFLICT DO NOTHING;
                    END IF;
                END $$;");
        }
        catch (Exception migrationSyncEx)
        {
            logger.LogWarning("Không thể đồng bộ bảng migration trước: {Message}", migrationSyncEx.Message);
        }

        await context.Database.MigrateAsync();
        logger.LogInformation("Database Migrations đã được áp dụng thành công.");

        // Nạp bộ dữ liệu mẫu phong phú khi khởi động nếu database chưa có đủ bài viết
        await DatabaseSeeder.SeedAsync(context, logger);
        await CategorySeeder.SeedAsync(context, logger);

        // Tự động sinh và nạp 1000 bài viết nếu cơ sở dữ liệu có dưới 500 bài viết
        var currentPostCount = await context.Posts.CountAsync();
        if (currentPostCount < 500)
        {
            await BulkPostSeeder.SeedBulkPostsAsync(context, logger, 1000);
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi xảy ra khi áp dụng database migrations hoặc seed data: {Message}", ex.Message);
    }
}


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

// 9. Development Seed Endpoint
app.MapPost("/api/admin/seed", async (SocialDbContext db, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("DatabaseSeeder");
    await DatabaseSeeder.SeedAsync(db, logger);
    return Results.Ok(new { message = "Đã nạp thành công bộ dữ liệu mẫu phong phú!" });
});

app.MapPost("/api/admin/seed-1000", async (SocialDbContext db, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("BulkPostSeeder");
    var inserted = await BulkPostSeeder.SeedBulkPostsAsync(db, logger, 1000);
    return Results.Ok(new { message = $"Đã nạp thành công {inserted} bài viết vào database!", count = inserted });
});

app.MapPost("/api/admin/seed-dotnet", async (SocialDbContext db, ILoggerFactory loggerFactory, [Microsoft.AspNetCore.Mvc.FromQuery] int count = 1000) =>
{
    var logger = loggerFactory.CreateLogger("DotNetPostSeeder");
    var inserted = await DotNetPostSeeder.SeedDotNetPostsAsync(db, logger, count);
    return Results.Ok(new { message = $"Đã nạp thành công {inserted} bài viết chuyên sâu về .NET, C# và Backend!", count = inserted });
});

app.MapGet("/api/admin/stats", async (SocialDbContext db) =>
{
    var userCount = await db.Users.CountAsync();
    var postCount = await db.Posts.CountAsync();
    var interestCount = await db.Interests.CountAsync();
    var postInterestCount = await db.PostInterests.CountAsync();
    return Results.Ok(new
    {
        totalUsers = userCount,
        totalPosts = postCount,
        totalInterests = interestCount,
        totalPostInterests = postInterestCount
    });
});

app.MapGet("/api/test/redis", async (Application.Common.ICacheService cacheService, IServiceProvider sp) =>
{
    var redis = sp.GetService<StackExchange.Redis.IConnectionMultiplexer>();
    var isConnected = redis?.IsConnected ?? false;
    var testKey = "test:ping";

    // Thử ghi vào cache
    await cacheService.SetAsync(testKey, new
    {
        message = "Redis Cache hoạt động tốt!",
        timestamp = DateTime.UtcNow
    }, TimeSpan.FromMinutes(5));

    // Thử đọc lại từ cache
    var readData = await cacheService.GetAsync<object>(testKey);

    return Results.Ok(new
    {
        status = "Success",
        redisMultiplexerRegistered = redis != null,
        isRedisConnected = isConnected,
        cacheReadWriteSuccess = readData != null,
        data = readData
    });
}).WithTags("Testing").AllowAnonymous();

app.Run();
