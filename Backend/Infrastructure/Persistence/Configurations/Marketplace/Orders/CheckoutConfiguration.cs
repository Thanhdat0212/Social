using Domain.Entities.Marketplace.Orders;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Marketplace;

public class CheckoutConfiguration : IEntityTypeConfiguration<Checkout>
{
    public void Configure(EntityTypeBuilder<Checkout> builder)
    {
        builder.ToTable("Checkouts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CheckoutNo)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(c => c.CheckoutNo)
            .IsUnique();

        builder.Property(c => c.BuyerUserId)
            .IsRequired();

        builder.HasIndex(c => c.BuyerUserId);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired()
            .HasDefaultValue(CheckoutStatus.Open);

        builder.HasIndex(c => c.Status);

        builder.Property(c => c.ItemsTotal)
            .IsRequired();

        builder.Property(c => c.DiscountTotal)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(c => c.GrandTotal)
            .IsRequired();

        builder.Property(c => c.IdempotencyKey)
            .HasMaxLength(128);

        // Chống bấm đúp (Idempotency) trên từng người mua
        builder.HasIndex(c => new { c.BuyerUserId, c.IdempotencyKey })
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");

        builder.Property(c => c.ExpiresAtUtc)
            .IsRequired();

        builder.Property(c => c.PaidAtUtc)
            .IsRequired(false);

        builder.HasOne(c => c.BuyerUser)
            .WithMany()
            .HasForeignKey(c => c.BuyerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Items)
            .WithOne(i => i.Checkout)
            .HasForeignKey(i => i.CheckoutId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CheckoutItemConfiguration : IEntityTypeConfiguration<CheckoutItem>
{
    public void Configure(EntityTypeBuilder<CheckoutItem> builder)
    {
        builder.ToTable("CheckoutItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.CheckoutId)
            .IsRequired();

        builder.Property(i => i.ShopId)
            .IsRequired();

        builder.Property(i => i.VariantId)
            .IsRequired();

        builder.Property(i => i.ProductId)
            .IsRequired();

        builder.Property(i => i.CategoryId)
            .IsRequired(false);

        builder.Property(i => i.Quantity)
            .IsRequired();

        builder.Property(i => i.UnitPrice)
            .IsRequired();

        builder.Property(i => i.LineTotal)
            .IsRequired();

        builder.Property(i => i.ProductNameSnapshot)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(i => i.VariantLabelSnapshot)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(i => i.SkuSnapshot)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.CommissionRateBp)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(i => i.CommissionAmount)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(i => i.ShopNet)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(i => i.DeliveryType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired()
            .HasDefaultValue(DeliveryType.CodePool);

        builder.HasOne(i => i.Shop)
            .WithMany()
            .HasForeignKey(i => i.ShopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Variant)
            .WithMany()
            .HasForeignKey(i => i.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
