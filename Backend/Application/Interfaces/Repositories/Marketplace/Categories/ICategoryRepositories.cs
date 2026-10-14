using Domain.Entities.Marketplace.Categories;
using Domain.Enums;

namespace Application.Interfaces.Repositories.Marketplace.Categories;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetAncestorsAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
    void Update(Category category);
    void Delete(Category category);
}

public interface IAttributeRepository
{
    Task<AttributeDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AttributeDefinition?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttributeDefinition>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AttributeDefinition attribute, CancellationToken cancellationToken = default);
    void Update(AttributeDefinition attribute);
    void Delete(AttributeDefinition attribute);

    Task<AttributeOption?> GetOptionByIdAsync(Guid optionId, CancellationToken cancellationToken = default);
    Task AddOptionAsync(AttributeOption option, CancellationToken cancellationToken = default);
    void DeleteOption(AttributeOption option);

    Task<IReadOnlyList<CategoryAttribute>> GetCategoryAttributesAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<CategoryAttribute?> GetCategoryAttributeAsync(Guid categoryId, Guid attributeId, CancellationToken cancellationToken = default);
    Task AssignCategoryAttributeAsync(CategoryAttribute categoryAttribute, CancellationToken cancellationToken = default);
    void RemoveCategoryAttribute(CategoryAttribute categoryAttribute);
}

public interface ICategoryRequestRepository
{
    Task<CategoryRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryRequest>> GetByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryRequest>> GetRequestsAsync(CategoryRequestStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CategoryRequestStatus? status, CancellationToken cancellationToken = default);
    Task AddAsync(CategoryRequest request, CancellationToken cancellationToken = default);
    void Update(CategoryRequest request);
}
