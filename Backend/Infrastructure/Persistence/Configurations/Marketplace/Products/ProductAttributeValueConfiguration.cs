using Domain.Entities.Marketplace.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Marketplace;

public class ProductAttributeValueConfiguration : IEntityTypeConfiguration<ProductAttributeValue>
{
    public void Configure(EntityTypeBuilder<ProductAttributeValue> builder)
    {
        builder.ToTable("ProductAttributeValues");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ProductId)
            .IsRequired();

        builder.Property(a => a.AttributeId)
            .IsRequired();

        builder.Property(a => a.OptionId)
            .IsRequired(false);

        builder.Property(a => a.TextValue)
            .HasMaxLength(2000);

        builder.Property(a => a.NumberValue)
            .HasPrecision(18, 4);

        builder.Property(a => a.BoolValue)
            .IsRequired(false);

        builder.HasIndex(a => new { a.ProductId, a.AttributeId });

        builder.HasOne(a => a.Attribute)
            .WithMany()
            .HasForeignKey(a => a.AttributeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Option)
            .WithMany()
            .HasForeignKey(a => a.OptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
