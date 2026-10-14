using Domain.Entities.Marketplace.Shops;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ShopConfiguration : IEntityTypeConfiguration<Shop>
{
    public void Configure(EntityTypeBuilder<Shop> builder)
    {
        builder.ToTable("Shops");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.OwnerUserId)
            .IsRequired();

        // Ràng buộc 1 User = 1 Shop (MVP bảo vệ tầng DB chống bấm đúp)
        builder.HasIndex(s => s.OwnerUserId)
            .IsUnique();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.Slug)
            .IsRequired()
            .HasMaxLength(160);

        builder.HasIndex(s => s.Slug)
            .IsUnique();

        builder.Property(s => s.LogoUrl)
            .HasMaxLength(500);

        builder.Property(s => s.Description)
            .HasMaxLength(2000);

        builder.Property(s => s.ContactEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(s => s.ApprovalStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired()
            .HasDefaultValue(ApprovalStatus.Pending);

        builder.Property(s => s.OperatingStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired()
            .HasDefaultValue(OperatingStatus.Open);

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired();

        builder.Property(s => s.UpdatedAtUtc)
            .IsRequired();

        builder.Property(s => s.DeletedAtUtc)
            .IsRequired(false);

        // Quan hệ 1-1 với User
        builder.HasOne(s => s.OwnerUser)
            .WithOne(u => u.Shop)
            .HasForeignKey<Shop>(s => s.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Quan hệ 1-N với ShopReview
        builder.HasMany(s => s.Reviews)
            .WithOne(r => r.Shop)
            .HasForeignKey(r => r.ShopId)
            .OnDelete(DeleteBehavior.Cascade);

        // Quan hệ 1-N với ShopSanction
        builder.HasMany(s => s.Sanctions)
            .WithOne(sn => sn.Shop)
            .HasForeignKey(sn => sn.ShopId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
