using Application.DTOs.Marketplace.Categories;
using Domain.Enums;

namespace Application.Interfaces.Marketplace.Categories;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryTreeNodeDto>> GetTreeAsync(CancellationToken cancellationToken = default);
    Task<CategoryDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryAttributeDto>> GetAttributesByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default);
}

public interface IAdminCategoryService
{
    Task<IReadOnlyList<CategoryTreeNodeDto>> GetAllTreeAsync(CancellationToken cancellationToken = default);
    Task<CategoryDetailDto> CreateCategoryAsync(CreateCategoryRequestDto request, CancellationToken cancellationToken = default);
    Task<CategoryDetailDto> UpdateCategoryAsync(Guid id, UpdateCategoryRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttributeDefinitionDto>> GetAllAttributesAsync(CancellationToken cancellationToken = default);
    Task<AttributeDefinitionDto> CreateAttributeAsync(CreateAttributeRequestDto request, CancellationToken cancellationToken = default);
    Task<AttributeDefinitionDto> UpdateAttributeAsync(Guid id, UpdateAttributeRequestDto request, CancellationToken cancellationToken = default);
    Task<AttributeOptionDto> AddAttributeOptionAsync(Guid attributeId, CreateAttributeOptionDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAttributeOptionAsync(Guid optionId, CancellationToken cancellationToken = default);

    Task<bool> AssignAttributeToCategoryAsync(Guid categoryId, AssignCategoryAttributeRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> RemoveAttributeFromCategoryAsync(Guid categoryId, Guid attributeId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CategoryRequestDto> Items, int Total)> GetCategoryRequestsAsync(CategoryRequestStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<CategoryRequestDto> ReviewCategoryRequestAsync(Guid id, ReviewCategoryRequestDto request, CancellationToken cancellationToken = default);
}

public interface ISellerCategoryService
{
    Task<CategoryRequestDto> RequestCategoryAsync(CreateCategoryRequestFromSellerDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryRequestDto>> GetMyCategoryRequestsAsync(CancellationToken cancellationToken = default);
}
