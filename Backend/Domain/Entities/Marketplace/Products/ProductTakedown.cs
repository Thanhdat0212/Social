using Domain.Common;
using Domain.Entities;

namespace Domain.Entities.Marketplace.Products;

/// <summary>
/// Bản ghi gỡ bỏ/khóa sản phẩm vi phạm bởi Quản trị viên (Product Takedown)
/// </summary>
public class ProductTakedown : BaseEntity
{
    public Guid ProductId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid AdminId { get; private set; }
    public DateTime? LiftedAtUtc { get; private set; }
    public Guid? LiftedByAdminId { get; private set; }

    // Navigation
    public Product? Product { get; private set; }
    public User? Admin { get; private set; }
    public User? LiftedByAdmin { get; private set; }

    protected ProductTakedown() { }

    public ProductTakedown(Guid productId, Guid adminId, string reason)
    {
        if (productId == Guid.Empty) throw new ArgumentException("ProductId không hợp lệ.", nameof(productId));
        if (adminId == Guid.Empty) throw new ArgumentException("AdminId không hợp lệ.", nameof(adminId));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Lý do gỡ bỏ sản phẩm không được để trống.", nameof(reason));

        ProductId = productId;
        AdminId = adminId;
        Reason = reason.Trim();
    }

    public void Lift(Guid adminId)
    {
        if (adminId == Guid.Empty) throw new ArgumentException("AdminId không hợp lệ.", nameof(adminId));
        if (LiftedAtUtc.HasValue) throw new InvalidOperationException("Lệnh gỡ bỏ sản phẩm này đã được dỡ bỏ trước đó.");

        LiftedAtUtc = DateTime.UtcNow;
        LiftedByAdminId = adminId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
