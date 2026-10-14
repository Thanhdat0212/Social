using Domain.Entities.Marketplace.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ProductTakedownConfiguration : IEntityTypeConfiguration<ProductTakedown>
{
    public void Configure(EntityTypeBuilder<ProductTakedown> builder)
    {
        builder.ToTable("ProductTakedowns");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ProductId)
            .IsRequired();

        builder.Property(t => t.AdminId)
            .IsRequired();

        builder.Property(t => t.Reason)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(t => t.LiftedAtUtc)
            .IsRequired(false);

        builder.Property(t => t.LiftedByAdminId)
            .IsRequired(false);

        builder.HasIndex(t => new { t.ProductId, t.LiftedAtUtc });

        builder.HasOne(t => t.Admin)
            .WithMany()
            .HasForeignKey(t => t.AdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.LiftedByAdmin)
            .WithMany()
            .HasForeignKey(t => t.LiftedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
