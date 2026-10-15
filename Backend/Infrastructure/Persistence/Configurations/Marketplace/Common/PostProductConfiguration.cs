using Domain.Entities.Marketplace;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Marketplace;

public class PostProductConfiguration : IEntityTypeConfiguration<PostProduct>
{
    public void Configure(EntityTypeBuilder<PostProduct> builder)
    {
        builder.ToTable("PostProducts");

        builder.HasKey(pp => new { pp.PostId, pp.ProductId });

        builder.HasOne(pp => pp.Post)
            .WithMany(p => p.PostProducts)
            .HasForeignKey(pp => pp.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pp => pp.Product)
            .WithMany(pr => pr.PostProducts)
            .HasForeignKey(pp => pp.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(pp => pp.CreatedAtUtc)
            .IsRequired();
    }
}
