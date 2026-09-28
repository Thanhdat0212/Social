using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class UserPreferenceConfiguration : IEntityTypeConfiguration<UserPreference>
{
    public void Configure(EntityTypeBuilder<UserPreference> builder)
    {
        builder.ToTable("UserPreferences");

        builder.HasKey(up => new { up.UserId, up.InterestId });

        builder.Property(up => up.Score)
            .IsRequired()
            .HasDefaultValue(1.0);

        builder.Property(up => up.CreatedAtUtc)
            .IsRequired();

        builder.Property(up => up.UpdatedAtUtc)
            .IsRequired();

        builder.HasOne(up => up.User)
            .WithMany(u => u.UserPreferences)
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(up => up.Interest)
            .WithMany(i => i.UserPreferences)
            .HasForeignKey(up => up.InterestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
