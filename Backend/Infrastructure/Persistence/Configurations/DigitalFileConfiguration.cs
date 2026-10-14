using Domain.Entities.Marketplace.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DigitalFileConfiguration : IEntityTypeConfiguration<DigitalFile>
{
    public void Configure(EntityTypeBuilder<DigitalFile> builder)
    {
        builder.ToTable("DigitalFiles");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.ShopId)
            .IsRequired();

        builder.Property(f => f.VariantId)
            .IsRequired();

        builder.Property(f => f.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(f => f.StoragePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(f => f.FileSizeBytes)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(f => f.ContentType)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("application/octet-stream");

        builder.Property(f => f.MaxDownloads)
            .IsRequired(false);

        builder.HasIndex(f => f.VariantId)
            .IsUnique();

        builder.HasOne(f => f.Shop)
            .WithMany()
            .HasForeignKey(f => f.ShopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Variant)
            .WithMany()
            .HasForeignKey(f => f.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
