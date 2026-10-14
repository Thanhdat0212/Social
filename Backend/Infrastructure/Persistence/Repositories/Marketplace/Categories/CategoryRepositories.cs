using Application.Interfaces.Repositories.Marketplace.Categories;
using Domain.Entities.Marketplace.Categories;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Marketplace.Categories;

public class CategoryRepository : ICategoryRepository
{
    private readonly SocialDbContext _context;

    public CategoryRepository(SocialDbContext context)
    {
        _context = context;
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Include(c => c.Parent)
            .Include(c => c.CategoryAttributes)
                .ThenInclude(ca => ca.AttributeDefinition)
                    .ThenInclude(a => a!.Options)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Include(c => c.Parent)
            .FirstOrDefaultAsync(c => c.Slug == slug.ToLower(), cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Depth)
            .ThenBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Include(c => c.Parent)
            .OrderBy(c => c.Depth)
            .ThenBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Where(c => c.ParentId == parentId)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetAncestorsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var ancestors = new List<Category>();
        var current = await _context.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

        while (current?.ParentId != null)
        {
            var parent = await _context.Categories
                .Include(c => c.CategoryAttributes)
                    .ThenInclude(ca => ca.AttributeDefinition)
                        .ThenInclude(a => a!.Options)
                .FirstOrDefaultAsync(c => c.Id == current.ParentId, cancellationToken);

            if (parent == null) break;
            ancestors.Insert(0, parent);
            current = parent;
        }

        return ancestors;
    }

    public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Categories.AnyAsync(c => c.Slug == slug.ToLower(), cancellationToken);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        await _context.Categories.AddAsync(category, cancellationToken);
    }

    public void Update(Category category)
    {
        _context.Categories.Update(category);
    }

    public void Delete(Category category)
    {
        _context.Categories.Remove(category);
    }
}

public class AttributeRepository : IAttributeRepository
{
    private readonly SocialDbContext _context;

    public AttributeRepository(SocialDbContext context)
    {
        _context = context;
    }

    public async Task<AttributeDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AttributeDefinitions
            .Include(a => a.Options.OrderBy(o => o.DisplayOrder))
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<AttributeDefinition?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.AttributeDefinitions
            .Include(a => a.Options.OrderBy(o => o.DisplayOrder))
            .FirstOrDefaultAsync(a => a.Code == code.ToLower(), cancellationToken);
    }

    public async Task<IReadOnlyList<AttributeDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AttributeDefinitions
            .Include(a => a.Options.OrderBy(o => o.DisplayOrder))
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AttributeDefinition attribute, CancellationToken cancellationToken = default)
    {
        await _context.AttributeDefinitions.AddAsync(attribute, cancellationToken);
    }

    public void Update(AttributeDefinition attribute)
    {
        _context.AttributeDefinitions.Update(attribute);
    }

    public void Delete(AttributeDefinition attribute)
    {
        _context.AttributeDefinitions.Remove(attribute);
    }

    public async Task<AttributeOption?> GetOptionByIdAsync(Guid optionId, CancellationToken cancellationToken = default)
    {
        return await _context.AttributeOptions.FirstOrDefaultAsync(o => o.Id == optionId, cancellationToken);
    }

    public async Task AddOptionAsync(AttributeOption option, CancellationToken cancellationToken = default)
    {
        await _context.AttributeOptions.AddAsync(option, cancellationToken);
    }

    public void DeleteOption(AttributeOption option)
    {
        _context.AttributeOptions.Remove(option);
    }

    public async Task<IReadOnlyList<CategoryAttribute>> GetCategoryAttributesAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.CategoryAttributes
            .Include(ca => ca.AttributeDefinition)
                .ThenInclude(a => a!.Options)
            .Where(ca => ca.CategoryId == categoryId)
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryAttribute?> GetCategoryAttributeAsync(Guid categoryId, Guid attributeId, CancellationToken cancellationToken = default)
    {
        return await _context.CategoryAttributes
            .Include(ca => ca.AttributeDefinition)
                .ThenInclude(a => a!.Options)
            .FirstOrDefaultAsync(ca => ca.CategoryId == categoryId && ca.AttributeId == attributeId, cancellationToken);
    }

    public async Task AssignCategoryAttributeAsync(CategoryAttribute categoryAttribute, CancellationToken cancellationToken = default)
    {
        await _context.CategoryAttributes.AddAsync(categoryAttribute, cancellationToken);
    }

    public void RemoveCategoryAttribute(CategoryAttribute categoryAttribute)
    {
        _context.CategoryAttributes.Remove(categoryAttribute);
    }
}

public class CategoryRequestRepository : ICategoryRequestRepository
{
    private readonly SocialDbContext _context;

    public CategoryRequestRepository(SocialDbContext context)
    {
        _context = context;
    }

    public async Task<CategoryRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CategoryRequests
            .Include(r => r.Shop)
            .Include(r => r.SuggestedParent)
            .Include(r => r.ResolvedCategory)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryRequest>> GetByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        return await _context.CategoryRequests
            .Include(r => r.SuggestedParent)
            .Include(r => r.ResolvedCategory)
            .Where(r => r.ShopId == shopId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryRequest>> GetRequestsAsync(CategoryRequestStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.CategoryRequests
            .Include(r => r.Shop)
            .Include(r => r.SuggestedParent)
            .Include(r => r.ResolvedCategory)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(CategoryRequestStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _context.CategoryRequests.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(CategoryRequest request, CancellationToken cancellationToken = default)
    {
        await _context.CategoryRequests.AddAsync(request, cancellationToken);
    }

    public void Update(CategoryRequest request)
    {
        _context.CategoryRequests.Update(request);
    }
}
