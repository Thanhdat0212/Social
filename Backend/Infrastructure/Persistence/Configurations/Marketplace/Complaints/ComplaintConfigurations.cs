using Domain.Entities.Marketplace.Complaints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Marketplace;

public class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
{
    public void Configure(EntityTypeBuilder<Complaint> builder)
    {
        builder.ToTable("Complaints");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Reason)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(c => c.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(c => c.FaultyQuantity)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(c => c.ShopResponseNote)
            .HasMaxLength(2000);

        builder.Property(c => c.AdminResolutionNote)
            .HasMaxLength(2000);

        builder.Property(c => c.ResolutionType)
            .HasMaxLength(64);

        builder.HasOne(c => c.ShopOrder)
            .WithMany()
            .HasForeignKey(c => c.ShopOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.OrderItem)
            .WithMany()
            .HasForeignKey(c => c.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.BuyerUser)
            .WithMany()
            .HasForeignKey(c => c.BuyerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Shop)
            .WithMany()
            .HasForeignKey(c => c.ShopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ResolvedByAdmin)
            .WithMany()
            .HasForeignKey(c => c.ResolvedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.ShopOrderId);
        builder.HasIndex(c => c.BuyerUserId);
        builder.HasIndex(c => c.ShopId);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.CreatedAtUtc);
    }
}

public class ComplaintEvidenceConfiguration : IEntityTypeConfiguration<ComplaintEvidence>
{
    public void Configure(EntityTypeBuilder<ComplaintEvidence> builder)
    {
        builder.ToTable("ComplaintEvidences");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FileUrl)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(e => e.FileType)
            .IsRequired()
            .HasMaxLength(32)
            .HasDefaultValue("Image");

        builder.Property(e => e.Note)
            .HasMaxLength(500);

        builder.HasOne(e => e.Complaint)
            .WithMany(c => c.Evidences)
            .HasForeignKey(e => e.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.ComplaintId);
    }
}

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("Refunds");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Amount)
            .IsRequired();

        builder.Property(r => r.Bearer)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(r => r.ReasonCode)
            .HasMaxLength(128);

        builder.Property(r => r.ProviderRefundId)
            .HasMaxLength(128);

        builder.Property(r => r.FailureReason)
            .HasMaxLength(500);

        builder.HasOne(r => r.Payment)
            .WithMany()
            .HasForeignKey(r => r.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ShopOrder)
            .WithMany()
            .HasForeignKey(r => r.ShopOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.OrderItem)
            .WithMany()
            .HasForeignKey(r => r.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Complaint)
            .WithMany()
            .HasForeignKey(r => r.ComplaintId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.ShopOrderId);
        builder.HasIndex(r => r.ComplaintId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.CreatedAtUtc);
    }
}

public class BuyerRiskFlagConfiguration : IEntityTypeConfiguration<BuyerRiskFlag>
{
    public void Configure(EntityTypeBuilder<BuyerRiskFlag> builder)
    {
        builder.ToTable("BuyerRiskFlags");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(f => f.Note)
            .HasMaxLength(500);

        builder.HasOne(f => f.BuyerUser)
            .WithMany()
            .HasForeignKey(f => f.BuyerUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => f.BuyerUserId);
        builder.HasIndex(f => f.CreatedAtUtc);
    }
}
