using Domain.Entities.Marketplace.Orders;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Marketplace;

public class ShopOrderConfiguration : IEntityTypeConfiguration<ShopOrder>
{
    public void Configure(EntityTypeBuilder<ShopOrder> builder)
    {
        builder.ToTable("ShopOrders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNo)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(o => o.OrderNo)
            .IsUnique();

        builder.Property(o => o.CheckoutId)
            .IsRequired();

        builder.HasIndex(o => o.CheckoutId);

        builder.Property(o => o.ShopId)
            .IsRequired();

        builder.HasIndex(o => o.ShopId);

        builder.Property(o => o.BuyerUserId)
            .IsRequired();

        builder.HasIndex(o => o.BuyerUserId);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired()
            .HasDefaultValue(OrderStatus.Delivered);

        builder.HasIndex(o => o.Status);

        builder.Property(o => o.Total)
            .IsRequired();

        builder.Property(o => o.ShopGross)
            .IsRequired();

        builder.Property(o => o.CommissionTotal)
            .IsRequired();

        builder.Property(o => o.ShopNet)
            .IsRequired();

        builder.Property(o => o.DeliveredAtUtc)
            .IsRequired(false);

        builder.Property(o => o.ComplaintWindowEndsAtUtc)
            .IsRequired(false);

        builder.Property(o => o.CompletedAtUtc)
            .IsRequired(false);

        builder.HasOne(o => o.Checkout)
            .WithMany()
            .HasForeignKey(o => o.CheckoutId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Shop)
            .WithMany()
            .HasForeignKey(o => o.ShopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.BuyerUser)
            .WithMany()
            .HasForeignKey(o => o.BuyerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Items)
            .WithOne(i => i.ShopOrder)
            .HasForeignKey(i => i.ShopOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Histories)
            .WithOne(h => h.ShopOrder)
            .HasForeignKey(h => h.ShopOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Fulfillments)
            .WithOne(f => f.ShopOrder)
            .HasForeignKey(f => f.ShopOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ShopOrderId)
            .IsRequired();

        builder.Property(i => i.VariantId)
            .IsRequired();

        builder.Property(i => i.ProductId)
            .IsRequired();

        builder.Property(i => i.CategoryId)
            .IsRequired(false);

        builder.Property(i => i.ProductName)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(i => i.VariantLabel)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(i => i.Sku)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.UnitPrice)
            .IsRequired();

        builder.Property(i => i.Quantity)
            .IsRequired();

        builder.Property(i => i.LineTotal)
            .IsRequired();

        builder.Property(i => i.ShopGross)
            .IsRequired();

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

        builder.Property(i => i.RefundedQty)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(i => i.DownloadCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne(i => i.Variant)
            .WithMany()
            .HasForeignKey(i => i.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Category)
            .WithMany()
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
