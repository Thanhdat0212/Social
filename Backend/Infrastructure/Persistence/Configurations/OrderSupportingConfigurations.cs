using Domain.Entities.Marketplace.Orders;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("OrderStatusHistories");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.ShopOrderId)
            .IsRequired();

        builder.Property(h => h.FromStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(h => h.ToStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(h => h.ActorType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(h => h.ActorId)
            .IsRequired(false);

        builder.Property(h => h.Reason)
            .HasMaxLength(1000);

        builder.HasIndex(h => h.ShopOrderId);
    }
}

public class FulfillmentConfiguration : IEntityTypeConfiguration<Fulfillment>
{
    public void Configure(EntityTypeBuilder<Fulfillment> builder)
    {
        builder.ToTable("Fulfillments");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.ShopOrderId)
            .IsRequired();

        builder.Property(f => f.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired()
            .HasDefaultValue(FulfillmentStatus.Fulfilled);

        builder.Property(f => f.DeliveredAtUtc)
            .IsRequired();

        builder.HasIndex(f => f.ShopOrderId);

        builder.HasMany(f => f.Items)
            .WithOne(i => i.Fulfillment)
            .HasForeignKey(i => i.FulfillmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class FulfillmentItemConfiguration : IEntityTypeConfiguration<FulfillmentItem>
{
    public void Configure(EntityTypeBuilder<FulfillmentItem> builder)
    {
        builder.ToTable("FulfillmentItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.FulfillmentId)
            .IsRequired();

        builder.Property(i => i.OrderItemId)
            .IsRequired();

        builder.Property(i => i.Quantity)
            .IsRequired();

        builder.HasIndex(i => i.FulfillmentId);

        builder.HasOne(i => i.OrderItem)
            .WithMany()
            .HasForeignKey(i => i.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
