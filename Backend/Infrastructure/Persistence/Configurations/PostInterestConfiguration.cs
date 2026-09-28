using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class PostInterestConfiguration : IEntityTypeConfiguration<PostInterest>
{
    public void Configure(EntityTypeBuilder<PostInterest> builder)
    {
        builder.ToTable("PostInterests");

        builder.HasKey(pi => new { pi.PostId, pi.InterestId });

        builder.Property(pi => pi.Confidence)
            .IsRequired()
            .HasDefaultValue(1.0);

        builder.Property(pi => pi.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(pi => pi.Post)
            .WithMany(p => p.PostInterests)
            .HasForeignKey(pi => pi.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pi => pi.Interest)
            .WithMany(i => i.PostInterests)
            .HasForeignKey(pi => pi.InterestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
