using Domain.Entities.Marketplace.Shops;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ShopReviewConfiguration : IEntityTypeConfiguration<ShopReview>
{
    public void Configure(EntityTypeBuilder<ShopReview> builder)
    {
        builder.ToTable("ShopReviews");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ShopId)
            .IsRequired();

        builder.Property(r => r.SubmissionNo)
            .IsRequired();

        // Ràng buộc duy nhất: Mỗi lần nộp có số thứ tự tăng dần duy nhất trong Shop
        builder.HasIndex(r => new { r.ShopId, r.SubmissionNo })
            .IsUnique();

        builder.Property(r => r.SnapshotJson)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(r => r.Decision)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired()
            .HasDefaultValue(ShopReviewDecision.Pending);

        builder.Property(r => r.ReasonCode)
            .HasMaxLength(100);

        builder.Property(r => r.ReasonNote)
            .HasMaxLength(2000);

        builder.Property(r => r.CanResubmit)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(r => r.ReviewedByAdminId)
            .IsRequired(false);

        builder.Property(r => r.ReviewedAtUtc)
            .IsRequired(false);

        builder.Property(r => r.CreatedAtUtc)
            .IsRequired();

        builder.Property(r => r.UpdatedAtUtc)
            .IsRequired();
    }
}
