using Domain.Entities.Marketplace.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class AttributeOptionConfiguration : IEntityTypeConfiguration<AttributeOption>
{
    public void Configure(EntityTypeBuilder<AttributeOption> builder)
    {
        builder.ToTable("AttributeOptions");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.AttributeId)
            .IsRequired();

        builder.Property(o => o.Value)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(o => o.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(o => o.CreatedAtUtc)
            .IsRequired();

        builder.Property(o => o.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(o => new { o.AttributeId, o.Slug })
            .IsUnique();

        builder.HasIndex(o => new { o.AttributeId, o.IsActive, o.DisplayOrder });

        builder.HasOne(o => o.AttributeDefinition)
            .WithMany(a => a.Options)
            .HasForeignKey(o => o.AttributeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
