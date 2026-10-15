using Domain.Entities.Marketplace.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Marketplace;

public class VariantOptionValueConfiguration : IEntityTypeConfiguration<VariantOptionValue>
{
    public void Configure(EntityTypeBuilder<VariantOptionValue> builder)
    {
        builder.ToTable("VariantOptionValues");

        builder.HasKey(vo => new { vo.VariantId, vo.OptionValueId });

        builder.HasOne(vo => vo.Variant)
            .WithMany(v => v.OptionValues)
            .HasForeignKey(vo => vo.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(vo => vo.OptionValue)
            .WithMany(ov => ov.VariantValues)
            .HasForeignKey(vo => vo.OptionValueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
