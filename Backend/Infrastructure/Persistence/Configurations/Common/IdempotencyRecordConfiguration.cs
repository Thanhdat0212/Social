using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Common;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.Endpoint)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.StatusCode)
            .IsRequired();

        builder.Property(x => x.ResponseBody)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        // Ràng buộc duy nhất: Một User trên cùng một Endpoint không thể có 2 bản ghi cùng Idempotency-Key
        builder.HasIndex(x => new { x.UserId, x.Key, x.Endpoint })
            .IsUnique();
    }
}
