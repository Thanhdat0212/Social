using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class SocialDbContext : DbContext
{
    public SocialDbContext(DbContextOptions<SocialDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<VerificationToken> VerificationTokens => Set<VerificationToken>();
    public DbSet<Interest> Interests => Set<Interest>();
    public DbSet<UserInterest> UserInterests => Set<UserInterest>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostInterest> PostInterests => Set<PostInterest>();
    public DbSet<PostLike> PostLikes => Set<PostLike>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<UserFollow> UserFollows => Set<UserFollow>();
    public DbSet<UserInteraction> UserInteractions => Set<UserInteraction>();
    public DbSet<PostEmbedding> PostEmbeddings => Set<PostEmbedding>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<Domain.Entities.Marketplace.Shops.Shop> Shops => Set<Domain.Entities.Marketplace.Shops.Shop>();
    public DbSet<Domain.Entities.Marketplace.Shops.ShopReview> ShopReviews => Set<Domain.Entities.Marketplace.Shops.ShopReview>();
    public DbSet<Domain.Entities.Marketplace.Shops.ShopSanction> ShopSanctions => Set<Domain.Entities.Marketplace.Shops.ShopSanction>();
    public DbSet<Domain.Entities.Marketplace.Categories.Category> Categories => Set<Domain.Entities.Marketplace.Categories.Category>();
    public DbSet<Domain.Entities.Marketplace.Categories.AttributeDefinition> AttributeDefinitions => Set<Domain.Entities.Marketplace.Categories.AttributeDefinition>();
    public DbSet<Domain.Entities.Marketplace.Categories.AttributeOption> AttributeOptions => Set<Domain.Entities.Marketplace.Categories.AttributeOption>();
    public DbSet<Domain.Entities.Marketplace.Categories.CategoryAttribute> CategoryAttributes => Set<Domain.Entities.Marketplace.Categories.CategoryAttribute>();
    public DbSet<Domain.Entities.Marketplace.Categories.CategoryRequest> CategoryRequests => Set<Domain.Entities.Marketplace.Categories.CategoryRequest>();
    public DbSet<Domain.Entities.Marketplace.Products.Product> Products => Set<Domain.Entities.Marketplace.Products.Product>();
    public DbSet<Domain.Entities.Marketplace.Products.ProductOption> ProductOptions => Set<Domain.Entities.Marketplace.Products.ProductOption>();
    public DbSet<Domain.Entities.Marketplace.Products.ProductOptionValue> ProductOptionValues => Set<Domain.Entities.Marketplace.Products.ProductOptionValue>();
    public DbSet<Domain.Entities.Marketplace.Products.ProductVariant> ProductVariants => Set<Domain.Entities.Marketplace.Products.ProductVariant>();
    public DbSet<Domain.Entities.Marketplace.Products.VariantOptionValue> VariantOptionValues => Set<Domain.Entities.Marketplace.Products.VariantOptionValue>();
    public DbSet<Domain.Entities.Marketplace.Products.ProductAttributeValue> ProductAttributeValues => Set<Domain.Entities.Marketplace.Products.ProductAttributeValue>();
    public DbSet<Domain.Entities.Marketplace.Products.ProductImage> ProductImages => Set<Domain.Entities.Marketplace.Products.ProductImage>();
    public DbSet<Domain.Entities.Marketplace.Products.ProductTakedown> ProductTakedowns => Set<Domain.Entities.Marketplace.Products.ProductTakedown>();
    public DbSet<Domain.Entities.Marketplace.Products.ProductReview> ProductReviews => Set<Domain.Entities.Marketplace.Products.ProductReview>();
    public DbSet<Domain.Entities.Marketplace.Inventory.DigitalCode> DigitalCodes => Set<Domain.Entities.Marketplace.Inventory.DigitalCode>();
    public DbSet<Domain.Entities.Marketplace.Inventory.DigitalFile> DigitalFiles => Set<Domain.Entities.Marketplace.Inventory.DigitalFile>();
    public DbSet<Domain.Entities.Marketplace.Orders.Cart> Carts => Set<Domain.Entities.Marketplace.Orders.Cart>();
    public DbSet<Domain.Entities.Marketplace.Orders.CartItem> CartItems => Set<Domain.Entities.Marketplace.Orders.CartItem>();
    public DbSet<Domain.Entities.Marketplace.Orders.Checkout> Checkouts => Set<Domain.Entities.Marketplace.Orders.Checkout>();
    public DbSet<Domain.Entities.Marketplace.Orders.CheckoutItem> CheckoutItems => Set<Domain.Entities.Marketplace.Orders.CheckoutItem>();
    public DbSet<Domain.Entities.Marketplace.Orders.ShopOrder> ShopOrders => Set<Domain.Entities.Marketplace.Orders.ShopOrder>();
    public DbSet<Domain.Entities.Marketplace.Orders.OrderItem> OrderItems => Set<Domain.Entities.Marketplace.Orders.OrderItem>();
    public DbSet<Domain.Entities.Marketplace.Orders.OrderStatusHistory> OrderStatusHistories => Set<Domain.Entities.Marketplace.Orders.OrderStatusHistory>();
    public DbSet<Domain.Entities.Marketplace.Orders.Fulfillment> Fulfillments => Set<Domain.Entities.Marketplace.Orders.Fulfillment>();
    public DbSet<Domain.Entities.Marketplace.Orders.FulfillmentItem> FulfillmentItems => Set<Domain.Entities.Marketplace.Orders.FulfillmentItem>();

    // Marketplace Payments & Ledger (M6)
    public DbSet<Domain.Entities.Marketplace.Payments.Payment> Payments => Set<Domain.Entities.Marketplace.Payments.Payment>();
    public DbSet<Domain.Entities.Marketplace.Ledger.LedgerAccount> LedgerAccounts => Set<Domain.Entities.Marketplace.Ledger.LedgerAccount>();
    public DbSet<Domain.Entities.Marketplace.Ledger.LedgerTransaction> LedgerTransactions => Set<Domain.Entities.Marketplace.Ledger.LedgerTransaction>();
    public DbSet<Domain.Entities.Marketplace.Ledger.LedgerEntry> LedgerEntries => Set<Domain.Entities.Marketplace.Ledger.LedgerEntry>();
    public DbSet<Domain.Entities.Marketplace.Ledger.ShopBalanceCache> ShopBalanceCaches => Set<Domain.Entities.Marketplace.Ledger.ShopBalanceCache>();

    // Marketplace Complaints & Refunds (M7)
    public DbSet<Domain.Entities.Marketplace.Complaints.Complaint> Complaints => Set<Domain.Entities.Marketplace.Complaints.Complaint>();
    public DbSet<Domain.Entities.Marketplace.Complaints.ComplaintEvidence> ComplaintEvidences => Set<Domain.Entities.Marketplace.Complaints.ComplaintEvidence>();
    public DbSet<Domain.Entities.Marketplace.Complaints.Refund> Refunds => Set<Domain.Entities.Marketplace.Complaints.Refund>();
    public DbSet<Domain.Entities.Marketplace.Complaints.BuyerRiskFlag> BuyerRiskFlags => Set<Domain.Entities.Marketplace.Complaints.BuyerRiskFlag>();

    // Marketplace Payouts (M8)
    public DbSet<Domain.Entities.Marketplace.Payouts.ShopBankAccount> ShopBankAccounts => Set<Domain.Entities.Marketplace.Payouts.ShopBankAccount>();
    public DbSet<Domain.Entities.Marketplace.Payouts.Payout> Payouts => Set<Domain.Entities.Marketplace.Payouts.Payout>();
    public DbSet<Domain.Entities.Marketplace.Payouts.PayoutStatusHistory> PayoutStatusHistories => Set<Domain.Entities.Marketplace.Payouts.PayoutStatusHistory>();

    // Marketplace Abuse Reports (M9.3)
    public DbSet<Domain.Entities.Marketplace.Reports.ProductReport> ProductReports => Set<Domain.Entities.Marketplace.Reports.ProductReport>();

    // Social Commerce
    public DbSet<Domain.Entities.Marketplace.PostProduct> PostProducts => Set<Domain.Entities.Marketplace.PostProduct>();

    // Marketplace Recommendations
    public DbSet<Domain.Entities.Marketplace.Recommendations.UserProductEvent> UserProductEvents => Set<Domain.Entities.Marketplace.Recommendations.UserProductEvent>();
    public DbSet<Domain.Entities.Marketplace.Recommendations.SearchHistory> SearchHistories => Set<Domain.Entities.Marketplace.Recommendations.SearchHistory>();
    public DbSet<Domain.Entities.Marketplace.Recommendations.ProductCoOccurrence> ProductCoOccurrences => Set<Domain.Entities.Marketplace.Recommendations.ProductCoOccurrence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SocialDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAtUtc == default)
                    entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                if (entry.Entity.UpdatedAtUtc == default)
                    entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
