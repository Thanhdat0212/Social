using Application.Interfaces.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly SocialDbContext _context;
    private IDbContextTransaction? _currentTransaction;

    private IUserRepository? _users;
    private IRefreshTokenRepository? _refreshTokens;
    private IVerificationTokenRepository? _verificationTokens;
    private IInterestRepository? _interests;
    private IUserInterestRepository? _userInterests;
    private IUserPreferenceRepository? _userPreferences;
    private IPostRepository? _posts;
    private IPostInterestRepository? _postInterests;
    private IPostLikeRepository? _postLikes;
    private ICommentRepository? _comments;
    private IUserFollowRepository? _userFollows;
    private IUserInteractionRepository? _userInteractions;
    private INotificationRepository? _notifications;
    private Application.Interfaces.Repositories.Marketplace.Shops.IShopRepository? _shops;
    private Application.Interfaces.Repositories.Marketplace.Shops.IShopReviewRepository? _shopReviews;
    private Application.Interfaces.Repositories.Marketplace.Shops.IShopSanctionRepository? _shopSanctions;
    private Application.Interfaces.Repositories.Marketplace.Categories.ICategoryRepository? _categories;
    private Application.Interfaces.Repositories.Marketplace.Categories.IAttributeRepository? _attributes;
    private Application.Interfaces.Repositories.Marketplace.Categories.ICategoryRequestRepository? _categoryRequests;
    private Application.Interfaces.Repositories.Marketplace.Products.IProductRepository? _products;
    private Application.Interfaces.Repositories.Marketplace.Products.IProductVariantRepository? _productVariants;
    private Application.Interfaces.Repositories.Marketplace.Products.IProductReviewRepository? _productReviews;
    private Application.Interfaces.Repositories.Marketplace.Inventory.IDigitalCodeRepository? _digitalCodes;
    private Application.Interfaces.Repositories.Marketplace.Inventory.IDigitalFileRepository? _digitalFiles;
    private Application.Interfaces.Repositories.Marketplace.Orders.ICartRepository? _carts;
    private Application.Interfaces.Repositories.Marketplace.Orders.ICheckoutRepository? _checkouts;
    private Application.Interfaces.Repositories.Marketplace.Orders.IOrderRepository? _orders;
    private Application.Interfaces.Repositories.Marketplace.Payments.IPaymentRepository? _payments;
    private Application.Interfaces.Repositories.Marketplace.Ledger.ILedgerRepository? _ledgers;
    private Application.Interfaces.Repositories.Marketplace.Complaints.IComplaintRepository? _complaints;
    private Application.Interfaces.Repositories.Marketplace.Complaints.IRefundRepository? _refunds;
    private Application.Interfaces.Repositories.Marketplace.Payouts.IShopBankAccountRepository? _shopBankAccounts;
    private Application.Interfaces.Repositories.Marketplace.Payouts.IPayoutRepository? _payouts;

    public UnitOfWork(SocialDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(_context);
    public IVerificationTokenRepository VerificationTokens => _verificationTokens ??= new VerificationTokenRepository(_context);
    public IInterestRepository Interests => _interests ??= new InterestRepository(_context);
    public IUserInterestRepository UserInterests => _userInterests ??= new UserInterestRepository(_context);
    public IUserPreferenceRepository UserPreferences => _userPreferences ??= new UserPreferenceRepository(_context);
    public IPostRepository Posts => _posts ??= new PostRepository(_context);
    public IPostInterestRepository PostInterests => _postInterests ??= new PostInterestRepository(_context);
    public IPostLikeRepository PostLikes => _postLikes ??= new PostLikeRepository(_context);
    public ICommentRepository Comments => _comments ??= new CommentRepository(_context);
    public IUserFollowRepository UserFollows => _userFollows ??= new UserFollowRepository(_context);
    public IUserInteractionRepository UserInteractions => _userInteractions ??= new UserInteractionRepository(_context);
    public INotificationRepository Notifications => _notifications ??= new NotificationRepository(_context);
    public Application.Interfaces.Repositories.Marketplace.Shops.IShopRepository Shops => _shops ??= new Infrastructure.Persistence.Repositories.Marketplace.Shops.ShopRepository(_context);
    public Application.Interfaces.Repositories.Marketplace.Shops.IShopReviewRepository ShopReviews => _shopReviews ??= new Infrastructure.Persistence.Repositories.Marketplace.Shops.ShopReviewRepository(_context);
    public Application.Interfaces.Repositories.Marketplace.Shops.IShopSanctionRepository ShopSanctions => _shopSanctions ??= new Infrastructure.Persistence.Repositories.Marketplace.Shops.ShopSanctionRepository(_context);
    public Application.Interfaces.Repositories.Marketplace.Categories.ICategoryRepository Categories => _categories ??= new Infrastructure.Persistence.Repositories.Marketplace.Categories.CategoryRepository(_context);
    public Application.Interfaces.Repositories.Marketplace.Categories.IAttributeRepository Attributes => _attributes ??= new Infrastructure.Persistence.Repositories.Marketplace.Categories.AttributeRepository(_context);
    public Application.Interfaces.Repositories.Marketplace.Categories.ICategoryRequestRepository CategoryRequests => _categoryRequests ??= new Infrastructure.Persistence.Repositories.Marketplace.Categories.CategoryRequestRepository(_context);
    public Application.Interfaces.Repositories.Marketplace.Products.IProductRepository Products => _products ??= new Infrastructure.Persistence.Repositories.Marketplace.Products.ProductRepository(_context);
    public Application.Interfaces.Repositories.Marketplace.Products.IProductVariantRepository ProductVariants => _productVariants ??= new Infrastructure.Persistence.Repositories.Marketplace.Products.ProductVariantRepository(_context);
    public Application.Interfaces.Repositories.Marketplace.Products.IProductReviewRepository ProductReviews => _productReviews ??= new Infrastructure.Persistence.Repositories.Marketplace.Products.ProductReviewRepository(_context);
    public Application.Interfaces.Repositories.Marketplace.Inventory.IDigitalCodeRepository DigitalCodes => _digitalCodes ??= new Infrastructure.Persistence.Repositories.Marketplace.Inventory.DigitalCodeRepository(_context);
    public Application.Interfaces.Repositories.Marketplace.Inventory.IDigitalFileRepository DigitalFiles => _digitalFiles ??= new Infrastructure.Persistence.Repositories.Marketplace.Inventory.DigitalFileRepository(_context);
    public Application.Interfaces.Repositories.Marketplace.Orders.ICartRepository Carts => _carts ??= new Infrastructure.Persistence.Repositories.Marketplace.Orders.CartRepository(_context);
    public Application.Interfaces.Repositories.Marketplace.Orders.ICheckoutRepository Checkouts => _checkouts ??= new Infrastructure.Persistence.Repositories.Marketplace.Orders.CheckoutRepository(_context);
    public Application.Interfaces.Repositories.Marketplace.Orders.IOrderRepository Orders => _orders ??= new Infrastructure.Persistence.Repositories.Marketplace.Orders.OrderRepository(_context);
    public Application.Interfaces.Repositories.Marketplace.Payments.IPaymentRepository Payments => _payments ??= new Infrastructure.Persistence.Repositories.Marketplace.Payments.PaymentRepository(_context);
    public Application.Interfaces.Repositories.Marketplace.Ledger.ILedgerRepository Ledgers => _ledgers ??= new Infrastructure.Persistence.Repositories.Marketplace.Ledger.LedgerRepository(_context);
    public Application.Interfaces.Repositories.Marketplace.Complaints.IComplaintRepository Complaints => _complaints ??= new Infrastructure.Persistence.Repositories.Marketplace.Complaints.ComplaintRepository(_context);
    public Application.Interfaces.Repositories.Marketplace.Complaints.IRefundRepository Refunds => _refunds ??= new Infrastructure.Persistence.Repositories.Marketplace.Complaints.RefundRepository(_context);
    public Application.Interfaces.Repositories.Marketplace.Payouts.IShopBankAccountRepository ShopBankAccounts => _shopBankAccounts ??= new Infrastructure.Persistence.Repositories.Marketplace.Payouts.ShopBankAccountRepository(_context);
    public Application.Interfaces.Repositories.Marketplace.Payouts.IPayoutRepository Payouts => _payouts ??= new Infrastructure.Persistence.Repositories.Marketplace.Payouts.PayoutRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            return;
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
            }
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
