using Application.Interfaces.Repositories.Marketplace.Products;
using Domain.Entities.Marketplace.Products;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Marketplace.Products;

public class ProductReviewRepository : IProductReviewRepository
{
    private readonly SocialDbContext _context;

    public ProductReviewRepository(SocialDbContext context)
    {
        _context = context;
    }

    public async Task<ProductReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ProductReviews
            .Include(r => r.BuyerUser)
            .Include(r => r.OrderItem)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<ProductReview?> GetByOrderItemIdAsync(Guid orderItemId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductReviews
            .Include(r => r.BuyerUser)
            .Include(r => r.OrderItem)
            .FirstOrDefaultAsync(r => r.OrderItemId == orderItemId, cancellationToken);
    }

    public async Task<bool> HasReviewedOrderItemAsync(Guid orderItemId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductReviews
            .AnyAsync(r => r.OrderItemId == orderItemId, cancellationToken);
    }

    public async Task AddAsync(ProductReview review, CancellationToken cancellationToken = default)
    {
        await _context.ProductReviews.AddAsync(review, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductReview>> GetByProductIdAsync(
        Guid productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProductReviews
            .Include(r => r.BuyerUser)
            .Include(r => r.OrderItem)
            .Where(r => r.ProductId == productId && !r.IsHidden)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductReviews
            .CountAsync(r => r.ProductId == productId && !r.IsHidden, cancellationToken);
    }

    public async Task<(double average, int count, Dictionary<int, int> starDistribution)> GetStatsByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var reviews = await _context.ProductReviews
            .Where(r => r.ProductId == productId && !r.IsHidden)
            .Select(r => r.Rating)
            .ToListAsync(cancellationToken);

        var count = reviews.Count;
        if (count == 0)
        {
            return (0.0, 0, new Dictionary<int, int>
            {
                { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }
            });
        }

        var average = reviews.Average();
        var distribution = new Dictionary<int, int>
        {
            { 1, reviews.Count(r => r == 1) },
            { 2, reviews.Count(r => r == 2) },
            { 3, reviews.Count(r => r == 3) },
            { 4, reviews.Count(r => r == 4) },
            { 5, reviews.Count(r => r == 5) }
        };

        return (average, count, distribution);
    }
}
