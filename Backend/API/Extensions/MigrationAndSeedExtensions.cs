using Application.Common;
using Domain.Entities.Marketplace.Inventory;
using Domain.Entities.Marketplace.Products;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace API.Extensions;

public static class MigrationAndSeedExtensions
{
    /// <summary>
    /// Tự động kiểm tra, đồng bộ schema migration và nạp bộ dữ liệu mẫu khởi động
    /// </summary>
    public static async Task ApplyMigrationsAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var context = services.GetRequiredService<SocialDbContext>();
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

            // Nạp bộ dữ liệu mẫu ban đầu (Users, Categories, Products & Orders)
            await DatabaseSeeder.SeedAsync(context, logger);
            await CategorySeeder.SeedAsync(context, logger);

            var cryptoService = services.GetRequiredService<ICryptoService>();
            await MarketplaceOrderSeeder.SeedOrdersForUserAsync(context, cryptoService, logger);
            await TapHoaMmoSeeder.SeedTapHoaMmoProductsAsync(context, cryptoService, logger);

            // Tự động sinh và nạp 1000 bài viết nếu cơ sở dữ liệu có dưới 500 bài viết
            var currentPostCount = await context.Posts.CountAsync();
            if (currentPostCount < 500)
            {
                await BulkPostSeeder.SeedBulkPostsAsync(context, logger, 1000);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi xảy ra khi áp dụng database migrations hoặc seed data: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Gom toàn bộ các endpoint hỗ trợ quá trình phát triển (Seed data, kiểm tra cache & thống kê)
    /// </summary>
    public static IEndpointRouteBuilder MapDevEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/dev")
            .WithTags("Development & Seeding");

        // 1. Seed dữ liệu nền tảng cơ bản
        group.MapPost("/seed", async (SocialDbContext db, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("DatabaseSeeder");
            await DatabaseSeeder.SeedAsync(db, logger);
            return Results.Ok(new { message = "Đã nạp thành công bộ dữ liệu mẫu phong phú!" });
        });

        // 2. Seed 1000 bài viết mạng xã hội
        group.MapPost("/seed-posts", async (SocialDbContext db, ILoggerFactory loggerFactory, [Microsoft.AspNetCore.Mvc.FromQuery] int count = 1000) =>
        {
            var logger = loggerFactory.CreateLogger("BulkPostSeeder");
            var inserted = await BulkPostSeeder.SeedBulkPostsAsync(db, logger, count);
            return Results.Ok(new { message = $"Đã nạp thành công {inserted} bài viết vào database!", count = inserted });
        });

        // 3. Seed bài viết chuyên sâu .NET & C#
        group.MapPost("/seed-dotnet", async (SocialDbContext db, ILoggerFactory loggerFactory, [Microsoft.AspNetCore.Mvc.FromQuery] int count = 1000) =>
        {
            var logger = loggerFactory.CreateLogger("DotNetPostSeeder");
            var inserted = await DotNetPostSeeder.SeedDotNetPostsAsync(db, logger, count);
            return Results.Ok(new { message = $"Đã nạp thành công {inserted} bài viết chuyên sâu về .NET, C# và Backend!", count = inserted });
        });

        // 4. Seed đơn hàng cho tài khoản người dùng
        group.MapPost("/seed-orders", async (SocialDbContext db, ICryptoService cryptoService, ILoggerFactory loggerFactory, [Microsoft.AspNetCore.Mvc.FromQuery] string? email) =>
        {
            var targetEmail = string.IsNullOrWhiteSpace(email) ? "Thanhdatvv05@gmail.com" : email.Trim();
            var logger = loggerFactory.CreateLogger("MarketplaceOrderSeeder");
            var inserted = await MarketplaceOrderSeeder.SeedOrdersForUserAsync(db, cryptoService, logger, targetEmail);
            return Results.Ok(new { message = $"Đã nạp thành công {inserted} đơn hàng cho tài khoản {targetEmail}!", count = inserted });
        });

        // Giữ lại endpoint cũ /api/seed/orders để tương thích ngược
        endpoints.MapPost("/api/seed/orders", async (SocialDbContext db, ICryptoService cryptoService, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("MarketplaceOrderSeeder");
            var inserted = await MarketplaceOrderSeeder.SeedOrdersForUserAsync(db, cryptoService, logger, "Thanhdatvv05@gmail.com");
            return Results.Ok(new { message = $"Đã nạp thành công {inserted} đơn hàng cho tài khoản Thanhdatvv05@gmail.com!", count = inserted });
        });

        // 5. Cập nhật và sửa ảnh sản phẩm & tồn kho
        group.MapPost("/fix-product-images", async (SocialDbContext db, ICryptoService cryptoService) =>
        {
            var brokenImgs = await db.ProductImages
                .Where(i => i.Url.Contains("photo-1677442136019"))
                .ToListAsync();

            foreach (var img in brokenImgs)
            {
                db.ProductImages.Remove(img);
                db.ProductImages.Add(new ProductImage(
                    img.ProductId,
                    "https://images.unsplash.com/photo-1620712943543-bcc4688e7485?w=800&auto=format&fit=crop&q=80",
                    0,
                    isCover: true));
            }

            var variants = await db.ProductVariants.ToListAsync();
            int addedStock = 0;
            foreach (var v in variants)
            {
                var count = await db.DigitalCodes.CountAsync(c => c.VariantId == v.Id && c.Status == DigitalCodeStatus.Available);
                if (count < 5)
                {
                    for (int i = 1; i <= 5 - count; i++)
                    {
                        var rawCode = $"{v.Sku}-STOCK-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
                        var enc = cryptoService.Encrypt(rawCode);
                        var hash = cryptoService.ComputeHash(rawCode);
                        var masked = cryptoService.MaskValue(rawCode);
                        db.DigitalCodes.Add(new DigitalCode(v.ShopId, v.Id, enc, hash, masked));
                        addedStock++;
                    }
                }
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { updatedImages = brokenImgs.Count, addedStock });
        });

        // 6. Thống kê nhanh dữ liệu
        endpoints.MapGet("/api/admin/stats", async (SocialDbContext db) =>
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

        // 7. Kiểm tra kết nối Redis Cache
        endpoints.MapGet("/api/test/redis", async (ICacheService cacheService, IServiceProvider sp) =>
        {
            var redis = sp.GetService<StackExchange.Redis.IConnectionMultiplexer>();
            var isConnected = redis?.IsConnected ?? false;
            var testKey = "test:ping";

            await cacheService.SetAsync(testKey, new
            {
                message = "Redis Cache hoạt động tốt!",
                timestamp = DateTime.UtcNow
            }, TimeSpan.FromMinutes(5));

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

        return endpoints;
    }
}
