namespace Application.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IVerificationTokenRepository VerificationTokens { get; }
    IInterestRepository Interests { get; }
    IUserInterestRepository UserInterests { get; }
    IUserPreferenceRepository UserPreferences { get; }
    IPostRepository Posts { get; }
    IPostInterestRepository PostInterests { get; }
    IPostLikeRepository PostLikes { get; }
    ICommentRepository Comments { get; }
    IUserFollowRepository UserFollows { get; }
    IUserInteractionRepository UserInteractions { get; }
    INotificationRepository Notifications { get; }
    Application.Interfaces.Repositories.Marketplace.Shops.IShopRepository Shops { get; }
    Application.Interfaces.Repositories.Marketplace.Shops.IShopReviewRepository ShopReviews { get; }
    Application.Interfaces.Repositories.Marketplace.Shops.IShopSanctionRepository ShopSanctions { get; }
    Application.Interfaces.Repositories.Marketplace.Categories.ICategoryRepository Categories { get; }
    Application.Interfaces.Repositories.Marketplace.Categories.IAttributeRepository Attributes { get; }
    Application.Interfaces.Repositories.Marketplace.Categories.ICategoryRequestRepository CategoryRequests { get; }
    Application.Interfaces.Repositories.Marketplace.Products.IProductRepository Products { get; }
    Application.Interfaces.Repositories.Marketplace.Products.IProductVariantRepository ProductVariants { get; }
    Application.Interfaces.Repositories.Marketplace.Products.IProductReviewRepository ProductReviews { get; }
    Application.Interfaces.Repositories.Marketplace.Inventory.IDigitalCodeRepository DigitalCodes { get; }
    Application.Interfaces.Repositories.Marketplace.Inventory.IDigitalFileRepository DigitalFiles { get; }
    Application.Interfaces.Repositories.Marketplace.Orders.ICartRepository Carts { get; }
    Application.Interfaces.Repositories.Marketplace.Orders.ICheckoutRepository Checkouts { get; }
    Application.Interfaces.Repositories.Marketplace.Orders.IOrderRepository Orders { get; }
    Application.Interfaces.Repositories.Marketplace.Payments.IPaymentRepository Payments { get; }
    Application.Interfaces.Repositories.Marketplace.Ledger.ILedgerRepository Ledgers { get; }
    Application.Interfaces.Repositories.Marketplace.Complaints.IComplaintRepository Complaints { get; }
    Application.Interfaces.Repositories.Marketplace.Complaints.IRefundRepository Refunds { get; }
    Application.Interfaces.Repositories.Marketplace.Payouts.IShopBankAccountRepository ShopBankAccounts { get; }
    Application.Interfaces.Repositories.Marketplace.Payouts.IPayoutRepository Payouts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
