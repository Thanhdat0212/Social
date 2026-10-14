using Domain.Entities.Marketplace.Inventory;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DigitalCodeConfiguration : IEntityTypeConfiguration<DigitalCode>
{
    public void Configure(EntityTypeBuilder<DigitalCode> builder)
    {
        builder.ToTable("DigitalCodes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ShopId)
            .IsRequired();

        builder.Property(c => c.VariantId)
            .IsRequired();

        builder.Property(c => c.ValueEncrypted)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(c => c.ValueHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(c => c.MaskedValue)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired()
            .HasDefaultValue(DigitalCodeStatus.Available);

        builder.Property(c => c.ReservedByCheckoutId)
            .IsRequired(false);

        builder.Property(c => c.OrderItemId)
            .IsRequired(false);

        builder.Property(c => c.DeliveredAtUtc)
            .IsRequired(false);

        // Chỉ mục duy nhất: Chống nạp trùng cùng 1 mã vào cùng 1 biến thể của Shop
        builder.HasIndex(c => new { c.ShopId, c.VariantId, c.ValueHash })
            .IsUnique();

        // Chỉ mục hiệu năng cực cao: Phục vụ đếm tồn kho và giữ chỗ mã nhanh chóng (FOR UPDATE SKIP LOCKED)
        builder.HasIndex(c => new { c.VariantId, c.Status });

        builder.HasIndex(c => c.ReservedByCheckoutId);
        builder.HasIndex(c => c.OrderItemId);

        builder.HasOne(c => c.Shop)
            .WithMany()
            .HasForeignKey(c => c.ShopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Variant)
            .WithMany()
            .HasForeignKey(c => c.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
