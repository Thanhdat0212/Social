using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class UserInteractionConfiguration : IEntityTypeConfiguration<UserInteraction>
{
    public void Configure(EntityTypeBuilder<UserInteraction> builder)
    {
        builder.ToTable("UserInteractions");

        builder.HasKey(ui => ui.Id);

        builder.Property(ui => ui.InteractionType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(ui => ui.Value)
            .IsRequired()
            .HasDefaultValue(1.0);

        builder.Property(ui => ui.Metadata)
            .HasMaxLength(1000);

        builder.Property(ui => ui.CreatedAtUtc)
            .IsRequired();

        builder.Property(ui => ui.UpdatedAtUtc)
            .IsRequired();

        builder.HasOne(ui => ui.User)
            .WithMany(u => u.Interactions)
            .HasForeignKey(ui => ui.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ui => ui.Post)
            .WithMany(p => p.Interactions)
            .HasForeignKey(ui => ui.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ui => ui.UserId);
        builder.HasIndex(ui => ui.PostId);
        builder.HasIndex(ui => ui.InteractionType);
        builder.HasIndex(ui => ui.CreatedAtUtc);
        builder.HasIndex(ui => new { ui.UserId, ui.InteractionType, ui.CreatedAtUtc });
    }
}
