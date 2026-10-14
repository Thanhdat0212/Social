using Domain.Entities.Marketplace.Products;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.ProductId)
            .IsRequired();

        builder.Property(v => v.ShopId)
            .IsRequired();

        builder.Property(v => v.Sku)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.SkuNormalized)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.CombinationKey)
            .IsRequired()
            .HasMaxLength(255)
            .HasDefaultValue("default");

        builder.Property(v => v.Price)
            .IsRequired();

        builder.Property(v => v.CompareAtPrice)
            .IsRequired(false);

        builder.Property(v => v.DeliveryType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired()
            .HasDefaultValue(DeliveryType.CodePool);

        builder.Property(v => v.TrackInventory)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(v => v.DeletedAtUtc)
            .IsRequired(false);

        // SKU unique trong từng Shop (loại trừ các biến thể đã xóa mềm)
        builder.HasIndex(v => new { v.ShopId, v.SkuNormalized })
            .IsUnique()
            .HasFilter("\"DeletedAtUtc\" IS NULL");

        // CombinationKey unique trong từng Product (chặn trùng tổ hợp variant)
        builder.HasIndex(v => new { v.ProductId, v.CombinationKey })
            .IsUnique()
            .HasFilter("\"DeletedAtUtc\" IS NULL");

        builder.HasOne(v => v.Shop)
            .WithMany()
            .HasForeignKey(v => v.ShopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(v => v.OptionValues)
            .WithOne(vo => vo.Variant)
            .HasForeignKey(vo => vo.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
