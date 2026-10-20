using Domain.Entities.Marketplace.Recommendations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Marketplace.Recommendations;

public class SearchHistoryConfiguration : IEntityTypeConfiguration<SearchHistory>
{
    public void Configure(EntityTypeBuilder<SearchHistory> builder)
    {
        builder.ToTable("SearchHistory");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.QueryText)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(s => s.NormalizedQuery)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(s => s.ResultCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired();

        // Khóa ngoại tới Users
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Index 1: Tìm kiếm lịch sử gần đây của User
        builder.HasIndex(s => new { s.UserId, s.CreatedAtUtc });

        // Index 2: Tìm kiếm theo từ khóa chuẩn hóa để gợi ý autocomplete / trending searches
        builder.HasIndex(s => s.NormalizedQuery);
    }
}
