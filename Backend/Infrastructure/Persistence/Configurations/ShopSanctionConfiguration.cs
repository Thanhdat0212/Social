using Domain.Entities.Marketplace.Shops;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ShopSanctionConfiguration : IEntityTypeConfiguration<ShopSanction>
{
    public void Configure(EntityTypeBuilder<ShopSanction> builder)
    {
        builder.ToTable("ShopSanctions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ShopId)
            .IsRequired();

        builder.HasIndex(s => s.ShopId);

        builder.Property(s => s.Type)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(s => s.Reason)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(s => s.StartAtUtc)
            .IsRequired();

        builder.Property(s => s.EndAtUtc)
            .IsRequired(false);

        builder.Property(s => s.CreatedByAdminId)
            .IsRequired();

        builder.Property(s => s.LiftedAtUtc)
            .IsRequired(false);

        builder.Property(s => s.LiftedByAdminId)
            .IsRequired(false);

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired();

        builder.Property(s => s.UpdatedAtUtc)
            .IsRequired();
    }
}
