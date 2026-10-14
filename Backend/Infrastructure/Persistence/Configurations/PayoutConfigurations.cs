using Domain.Entities.Marketplace.Payouts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ShopBankAccountConfiguration : IEntityTypeConfiguration<ShopBankAccount>
{
    public void Configure(EntityTypeBuilder<ShopBankAccount> builder)
    {
        builder.ToTable("ShopBankAccounts");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BankCode)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(b => b.BankName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(b => b.AccountNumberEncrypted)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(b => b.AccountNumberMasked)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(b => b.AccountHolderName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.HasOne(b => b.Shop)
            .WithMany()
            .HasForeignKey(b => b.ShopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => b.ShopId);
        builder.HasIndex(b => b.Status);
    }
}

public class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    public void Configure(EntityTypeBuilder<Payout> builder)
    {
        builder.ToTable("Payouts");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount)
            .IsRequired();

        builder.Property(p => p.Fee)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.NetAmount)
            .IsRequired();

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(p => p.RequestIdempotencyKey)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(p => p.BankTransactionRef)
            .HasMaxLength(128);

        builder.Property(p => p.FailureReason)
            .HasMaxLength(500);

        builder.Property(p => p.AdminNote)
            .HasMaxLength(500);

        builder.HasOne(p => p.Shop)
            .WithMany()
            .HasForeignKey(p => p.ShopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ShopBankAccount)
            .WithMany()
            .HasForeignKey(p => p.ShopBankAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.StatusHistories)
            .WithOne(h => h.Payout)
            .HasForeignKey(h => h.PayoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.RequestIdempotencyKey)
            .IsUnique();

        // Partial unique index: Mỗi Shop chỉ có tối đa 1 lệnh Payout đang active (Requested, Approved, Processing, Unknown)
        builder.HasIndex(p => p.ShopId)
            .IsUnique()
            .HasFilter("\"Status\" IN ('Requested', 'Approved', 'Processing', 'Unknown')");

        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.CreatedAtUtc);
    }
}

public class PayoutStatusHistoryConfiguration : IEntityTypeConfiguration<PayoutStatusHistory>
{
    public void Configure(EntityTypeBuilder<PayoutStatusHistory> builder)
    {
        builder.ToTable("PayoutStatusHistories");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.PreviousStatus)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(h => h.NewStatus)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(h => h.ActorType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(h => h.Note)
            .HasMaxLength(500);

        builder.HasIndex(h => h.PayoutId);
        builder.HasIndex(h => h.CreatedAtUtc);
    }
}
