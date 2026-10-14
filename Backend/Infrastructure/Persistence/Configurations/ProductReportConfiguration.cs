using Domain.Entities.Marketplace.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ProductReportConfiguration : IEntityTypeConfiguration<ProductReport>
{
    public void Configure(EntityTypeBuilder<ProductReport> builder)
    {
        builder.ToTable("ProductReports");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Reason)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(r => r.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(r => r.EvidenceUrl)
            .HasMaxLength(1024);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(r => r.AdminResolutionNote)
            .HasMaxLength(2000);

        builder.HasOne(r => r.ReporterUser)
            .WithMany()
            .HasForeignKey(r => r.ReporterUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Shop)
            .WithMany()
            .HasForeignKey(r => r.ShopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ResolvedByAdmin)
            .WithMany()
            .HasForeignKey(r => r.ResolvedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        // Chống spam: Mỗi User chỉ có 1 báo cáo Pending trên cùng 1 sản phẩm
        builder.HasIndex(r => new { r.ReporterUserId, r.ProductId })
            .IsUnique()
            .HasFilter("\"Status\" = 'Pending'");

        builder.HasIndex(r => r.ProductId);
        builder.HasIndex(r => r.ShopId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.CreatedAtUtc);
    }
}
