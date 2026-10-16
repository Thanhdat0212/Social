using Application.Common;
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

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi xảy ra khi áp dụng database migrations hoặc seed data: {Message}", ex.Message);
        }
    }
}
