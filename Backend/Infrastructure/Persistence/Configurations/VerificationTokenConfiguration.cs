using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class VerificationTokenConfiguration : IEntityTypeConfiguration<VerificationToken>
{
    public void Configure(EntityTypeBuilder<VerificationToken> builder)
    {
        builder.ToTable("VerificationTokens");

        builder.HasKey(vt => vt.Id);

        builder.Property(vt => vt.TokenHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(vt => vt.Purpose)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(vt => vt.ExpiresAtUtc)
            .IsRequired();

        builder.Property(vt => vt.CreatedAtUtc)
            .IsRequired();

        builder.Property(vt => vt.ConsumedAtUtc);

        builder.HasIndex(vt => new { vt.UserId, vt.Purpose });

        builder.HasOne(vt => vt.User)
            .WithMany(u => u.VerificationTokens)
            .HasForeignKey(vt => vt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
