using Domain.Entities.Marketplace.Products;

namespace Application.Interfaces.Repositories.Marketplace.Products;

public interface IProductReviewRepository
{
    Task<ProductReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductReview?> GetByOrderItemIdAsync(Guid orderItemId, CancellationToken cancellationToken = default);
    Task<bool> HasReviewedOrderItemAsync(Guid orderItemId, CancellationToken cancellationToken = default);
    Task AddAsync(ProductReview review, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductReview>> GetByProductIdAsync(Guid productId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetCountByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<(double average, int count, Dictionary<int, int> starDistribution)> GetStatsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
}
