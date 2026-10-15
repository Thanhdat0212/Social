using Domain.Entities.Marketplace.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Marketplace;

public class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.ToTable("ProductReviews");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ProductId)
            .IsRequired();

        builder.Property(r => r.ShopId)
            .IsRequired();

        builder.Property(r => r.BuyerUserId)
            .IsRequired();

        builder.Property(r => r.ShopOrderId)
            .IsRequired();

        builder.Property(r => r.OrderItemId)
            .IsRequired();

        // Ràng buộc duy nhất: Mỗi món hàng OrderItemId chỉ được đánh giá 1 lần duy nhất
        builder.HasIndex(r => r.OrderItemId)
            .IsUnique();

        // Index tìm kiếm theo sản phẩm và trạng thái hiển thị
        builder.HasIndex(r => new { r.ProductId, r.IsHidden });
        builder.HasIndex(r => r.ShopId);
        builder.HasIndex(r => r.BuyerUserId);

        builder.Property(r => r.Rating)
            .IsRequired();

        builder.Property(r => r.Comment)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(r => r.IsVerifiedPurchase)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(r => r.SellerReply)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(r => r.SellerRepliedAtUtc)
            .IsRequired(false);

        builder.Property(r => r.IsHidden)
            .IsRequired()
            .HasDefaultValue(false);

        // Foreign keys
        builder.HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Shop)
            .WithMany()
            .HasForeignKey(r => r.ShopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.BuyerUser)
            .WithMany()
            .HasForeignKey(r => r.BuyerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ShopOrder)
            .WithMany()
            .HasForeignKey(r => r.ShopOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.OrderItem)
            .WithMany()
            .HasForeignKey(r => r.OrderItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
