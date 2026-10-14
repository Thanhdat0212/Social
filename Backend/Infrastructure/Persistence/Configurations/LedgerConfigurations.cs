using Domain.Entities.Marketplace.Ledger;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class LedgerAccountConfiguration : IEntityTypeConfiguration<LedgerAccount>
{
    public void Configure(EntityTypeBuilder<LedgerAccount> builder)
    {
        builder.ToTable("LedgerAccounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(a => a.Code)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(a => a.Currency)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("VND");

        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne(a => a.Shop)
            .WithMany()
            .HasForeignKey(a => a.ShopId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique Code
        builder.HasIndex(a => a.Code)
            .IsUnique();

        builder.HasIndex(a => new { a.Type, a.ShopId });
    }
}

public class LedgerTransactionConfiguration : IEntityTypeConfiguration<LedgerTransaction>
{
    public void Configure(EntityTypeBuilder<LedgerTransaction> builder)
    {
        builder.ToTable("LedgerTransactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(t => t.RefType)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(t => t.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(t => t.Memo)
            .HasMaxLength(500);

        // Unique Idempotency Key - bảo đảm mỗi nghiệp vụ chỉ ghi sổ đúng 1 lần
        builder.HasIndex(t => t.IdempotencyKey)
            .IsUnique();

        builder.HasIndex(t => new { t.RefType, t.RefId });
        builder.HasIndex(t => t.CreatedAtUtc);
    }
}

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("LedgerEntries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Debit)
            .IsRequired();

        builder.Property(e => e.Credit)
            .IsRequired();

        builder.HasOne(e => e.Transaction)
            .WithMany(t => t.Entries)
            .HasForeignKey(e => e.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Account)
            .WithMany(a => a.Entries)
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.TransactionId);
        builder.HasIndex(e => e.AccountId);
        builder.HasIndex(e => e.CreatedAtUtc);
    }
}

public class ShopBalanceCacheConfiguration : IEntityTypeConfiguration<ShopBalanceCache>
{
    public void Configure(EntityTypeBuilder<ShopBalanceCache> builder)
    {
        builder.ToTable("ShopBalanceCaches");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.PendingBalance)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(b => b.AvailableBalance)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(b => b.PayoutPendingBalance)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(b => b.LifetimeGrossRevenue)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(b => b.LifetimeCommissionPaid)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(b => b.Version)
            .IsConcurrencyToken()
            .IsRequired()
            .HasDefaultValue(1);

        builder.HasOne(b => b.Shop)
            .WithOne()
            .HasForeignKey<ShopBalanceCache>(b => b.ShopId)
            .OnDelete(DeleteBehavior.Cascade);

        // Mỗi shop chỉ có 1 bản ghi cache số dư duy nhất
        builder.HasIndex(b => b.ShopId)
            .IsUnique();
    }
}
