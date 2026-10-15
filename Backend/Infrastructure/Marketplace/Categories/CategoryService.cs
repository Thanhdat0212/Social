using Application.Common;
using Application.DTOs.Marketplace.Categories;
using Application.Interfaces.Marketplace.Categories;
using Application.Interfaces.Repositories;
using Domain.Entities.Marketplace.Categories;
using Infrastructure.External.Caching;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Categories;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<CategoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CategoryTreeNodeDto>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cacheService.GetAsync<IReadOnlyList<CategoryTreeNodeDto>>(CacheKeys.CategoriesTree, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        var allActive = await _unitOfWork.Categories.GetAllActiveAsync(cancellationToken);
        var tree = BuildTree(allActive);

        _cacheService.Set(CacheKeys.CategoriesTree, tree, TimeSpan.FromHours(24));
        return tree;
    }

    public async Task<CategoryDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
        if (category == null) return null;

        var attributes = await GetAttributesByCategoryIdAsync(id, cancellationToken);

        return new CategoryDetailDto
        {
            Id = category.Id,
            ParentId = category.ParentId,
            ParentName = category.Parent?.Name,
            Name = category.Name,
            Slug = category.Slug,
            Path = category.Path,
            Depth = category.Depth,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,
            ProductType = category.ProductType.ToString(),
            ModerationPolicy = category.ModerationPolicy.ToString(),
            CommissionRateBp = category.CommissionRateBp,
            CreatedAtUtc = category.CreatedAtUtc,
            UpdatedAtUtc = category.UpdatedAtUtc,
            Attributes = attributes.ToList()
        };
    }

    public async Task<IReadOnlyList<CategoryAttributeDto>> GetAttributesByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.CategoryAttributes(categoryId);
        var cached = await _cacheService.GetAsync<IReadOnlyList<CategoryAttributeDto>>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        // 1. Lấy danh sách tổ tiên (từ root đến danh mục cha)
        var ancestors = await _unitOfWork.Categories.GetAncestorsAsync(categoryId, cancellationToken);
        
        // 2. Lấy thuộc tính trực tiếp của danh mục hiện tại
        var currentAttributes = await _unitOfWork.Attributes.GetCategoryAttributesAsync(categoryId, cancellationToken);

        // 3. Gom và thừa kế thuộc tính (Tổ tiên -> Danh mục con ghi đè nếu trùng AttributeId)
        var attributeDict = new Dictionary<Guid, CategoryAttributeDto>();

        foreach (var ancestor in ancestors)
        {
            foreach (var ca in ancestor.CategoryAttributes)
            {
                if (ca.AttributeDefinition == null) continue;
                attributeDict[ca.AttributeId] = MapToDto(ca, ancestor.Id);
            }
        }

        foreach (var ca in currentAttributes)
        {
            if (ca.AttributeDefinition == null) continue;
            // Ghi đè hoặc thêm mới từ danh mục hiện tại
            attributeDict[ca.AttributeId] = MapToDto(ca, null);
        }

        var result = attributeDict.Values.OrderBy(a => a.Name).ToList();

        _cacheService.Set(cacheKey, (IReadOnlyList<CategoryAttributeDto>)result, TimeSpan.FromHours(12));
        return result;
    }

    private static List<CategoryTreeNodeDto> BuildTree(IReadOnlyList<Category> categories)
    {
        var lookup = categories.ToLookup(c => c.ParentId);

        List<CategoryTreeNodeDto> GetChildren(Guid? parentId)
        {
            return lookup[parentId]
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new CategoryTreeNodeDto
                {
                    Id = c.Id,
                    ParentId = c.ParentId,
                    Name = c.Name,
                    Slug = c.Slug,
                    Path = c.Path,
                    Depth = c.Depth,
                    DisplayOrder = c.DisplayOrder,
                    IsActive = c.IsActive,
                    ProductType = c.ProductType.ToString(),
                    ModerationPolicy = c.ModerationPolicy.ToString(),
                    CommissionRateBp = c.CommissionRateBp,
                    Children = GetChildren(c.Id)
                })
                .ToList();
        }

        return GetChildren(null);
    }

    private static CategoryAttributeDto MapToDto(CategoryAttribute ca, Guid? inheritedFromId)
    {
        var def = ca.AttributeDefinition!;
        return new CategoryAttributeDto
        {
            AttributeId = ca.AttributeId,
            Code = def.Code,
            Name = def.Name,
            DataType = def.DataType.ToString(),
            Unit = def.Unit,
            Requirement = ca.Requirement.ToString(),
            IsFilterable = ca.IsFilterable,
            IsVariantAxis = ca.IsVariantAxis,
            InheritedFromCategoryId = inheritedFromId,
            Options = def.Options.Where(o => o.IsActive).OrderBy(o => o.DisplayOrder).Select(o => new AttributeOptionDto
            {
                Id = o.Id,
                AttributeId = o.AttributeId,
                Value = o.Value,
                Slug = o.Slug,
                DisplayOrder = o.DisplayOrder,
                IsActive = o.IsActive
            }).ToList()
        };
    }
}
