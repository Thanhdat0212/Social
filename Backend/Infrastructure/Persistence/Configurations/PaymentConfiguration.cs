using Domain.Entities.Marketplace.Payments;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Provider)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(p => p.ProviderTxnId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(p => p.Method)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(p => p.Amount)
            .IsRequired();

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("VND");

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(p => p.RawPayloadRef)
            .HasMaxLength(512);

        builder.Property(p => p.QrCodeUrl)
            .HasMaxLength(1024);

        builder.Property(p => p.PaymentUrl)
            .HasMaxLength(1024);

        // Foreign Key to Checkout
        builder.HasOne(p => p.Checkout)
            .WithMany()
            .HasForeignKey(p => p.CheckoutId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique index: Mỗi giao dịch từ provider chỉ xuất hiện 1 lần
        builder.HasIndex(p => new { p.Provider, p.ProviderTxnId })
            .IsUnique();

        // Partial Unique Index: Mỗi Checkout chỉ có tối đa 1 Payment Succeeded
        builder.HasIndex(p => p.CheckoutId)
            .IsUnique()
            .HasFilter("\"Status\" = 'Succeeded'");

        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.CreatedAtUtc);
    }
}
