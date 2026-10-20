using Domain.Entities.Marketplace.Recommendations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Marketplace.Recommendations;

public class UserProductEventConfiguration : IEntityTypeConfiguration<UserProductEvent>
{
    public void Configure(EntityTypeBuilder<UserProductEvent> builder)
    {
        builder.ToTable("UserProductEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SessionId)
            .HasMaxLength(64)
            .IsRequired(false);

        builder.Property(e => e.EventType)
            .IsRequired();

        builder.Property(e => e.DwellTimeSeconds)
            .IsRequired(false);

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired();

        // Khóa ngoại tới bảng Users (Xóa User thì giữ lại event nhưng set UserId = null)
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Khóa ngoại tới bảng Products (Xóa sản phẩm thì xóa các events liên quan)
        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index 1: Tối ưu truy vấn đọc hành vi tương tác gần đây theo User (Time-decay scoring)
        builder.HasIndex(e => new { e.UserId, e.CreatedAtUtc });

        // Index 2: Tối ưu tổng hợp độ hot của sản phẩm theo loại tương tác trong khoảng thời gian
        builder.HasIndex(e => new { e.ProductId, e.EventType, e.CreatedAtUtc });

        // Index 3: Tối ưu truy vấn theo phiên cho khách vãng lai chưa đăng nhập
        builder.HasIndex(e => new { e.SessionId, e.CreatedAtUtc });
    }
}
