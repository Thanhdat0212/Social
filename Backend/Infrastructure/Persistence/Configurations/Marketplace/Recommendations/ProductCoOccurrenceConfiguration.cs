using Domain.Entities.Marketplace.Recommendations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Marketplace.Recommendations;

public class ProductCoOccurrenceConfiguration : IEntityTypeConfiguration<ProductCoOccurrence>
{
    public void Configure(EntityTypeBuilder<ProductCoOccurrence> builder)
    {
        builder.ToTable("ProductCoOccurrences");

        // Khóa chính phức hợp hai chiều ProductAId - ProductBId
        builder.HasKey(c => new { c.ProductAId, c.ProductBId });

        builder.Property(c => c.CoOccurrenceCount)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(c => c.Score)
            .IsRequired()
            .HasDefaultValue(0.0);

        builder.Property(c => c.UpdatedAtUtc)
            .IsRequired();

        // Khóa ngoại ProductA
        builder.HasOne(c => c.ProductA)
            .WithMany()
            .HasForeignKey(c => c.ProductAId)
            .OnDelete(DeleteBehavior.Cascade);

        // Khóa ngoại ProductB
        builder.HasOne(c => c.ProductB)
            .WithMany()
            .HasForeignKey(c => c.ProductBId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index: Truy vấn nhanh các sản phẩm mua cùng với ProductA sắp xếp theo Score giảm dần
        builder.HasIndex(c => new { c.ProductAId, c.Score });
    }
}
