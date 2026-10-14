using Domain.Entities.Marketplace.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CategoryAttributeConfiguration : IEntityTypeConfiguration<CategoryAttribute>
{
    public void Configure(EntityTypeBuilder<CategoryAttribute> builder)
    {
        builder.ToTable("CategoryAttributes");

        builder.HasKey(ca => new { ca.CategoryId, ca.AttributeId });

        builder.Property(ca => ca.Requirement)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(ca => ca.IsFilterable)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ca => ca.IsVariantAxis)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ca => ca.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(ca => ca.Category)
            .WithMany(c => c.CategoryAttributes)
            .HasForeignKey(ca => ca.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ca => ca.AttributeDefinition)
            .WithMany(a => a.CategoryAttributes)
            .HasForeignKey(ca => ca.AttributeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
