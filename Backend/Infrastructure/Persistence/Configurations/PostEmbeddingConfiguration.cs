using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class PostEmbeddingConfiguration : IEntityTypeConfiguration<PostEmbedding>
{
    public void Configure(EntityTypeBuilder<PostEmbedding> builder)
    {
        builder.ToTable("PostEmbeddings");

        builder.HasKey(pe => pe.PostId);

        builder.Property(pe => pe.Values)
            .IsRequired()
            .HasColumnType("real[]");

        builder.Property(pe => pe.Model)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pe => pe.CreatedAtUtc)
            .IsRequired();

        builder.Property(pe => pe.UpdatedAtUtc)
            .IsRequired();

        builder.HasOne(pe => pe.Post)
            .WithOne(p => p.Embedding)
            .HasForeignKey<PostEmbedding>(pe => pe.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
