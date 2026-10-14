using Domain.Entities.Marketplace.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ProductOptionValueConfiguration : IEntityTypeConfiguration<ProductOptionValue>
{
    public void Configure(EntityTypeBuilder<ProductOptionValue> builder)
    {
        builder.ToTable("ProductOptionValues");

        builder.HasKey(ov => ov.Id);

        builder.Property(ov => ov.OptionId)
            .IsRequired();

        builder.Property(ov => ov.Value)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ov => ov.Position)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasIndex(ov => new { ov.OptionId, ov.Position });
    }
}
