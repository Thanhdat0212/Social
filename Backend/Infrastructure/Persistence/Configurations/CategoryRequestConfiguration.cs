using Domain.Entities.Marketplace.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CategoryRequestConfiguration : IEntityTypeConfiguration<CategoryRequest>
{
    public void Configure(EntityTypeBuilder<CategoryRequest> builder)
    {
        builder.ToTable("CategoryRequests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ShopId)
            .IsRequired();

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(r => r.ReasonNote)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(r => r.AdminNote)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(r => r.CreatedAtUtc)
            .IsRequired();

        builder.Property(r => r.UpdatedAtUtc)
            .IsRequired();

        builder.Property(r => r.ReviewedAtUtc)
            .IsRequired(false);

        builder.HasIndex(r => r.ShopId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.CreatedAtUtc);

        builder.HasOne(r => r.Shop)
            .WithMany()
            .HasForeignKey(r => r.ShopId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.SuggestedParent)
            .WithMany()
            .HasForeignKey(r => r.SuggestedParentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.ResolvedCategory)
            .WithMany()
            .HasForeignKey(r => r.ResolvedCategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
