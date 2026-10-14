using System.Text.Json;
using Application.Common;
using Application.DTOs.Marketplace.Categories;
using Application.DTOs.Notifications;
using Application.Interfaces;
using Application.Interfaces.Marketplace.Categories;
using Application.Interfaces.Repositories;
using Domain.Entities.Marketplace.Categories;
using Domain.Exceptions;
using Infrastructure.Caching;
using Infrastructure.Services.Marketplace.Shops;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Marketplace.Categories;

public class AdminCategoryService : IAdminCategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AdminCategoryService> _logger;

    public AdminCategoryService(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        INotificationService notificationService,
        ICurrentUserService currentUserService,
        ILogger<AdminCategoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CategoryTreeNodeDto>> GetAllTreeAsync(CancellationToken cancellationToken = default)
    {
        var all = await _unitOfWork.Categories.GetAllAsync(cancellationToken);
        return BuildTree(all);
    }

    public async Task<CategoryDetailDto> CreateCategoryAsync(CreateCategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        var slug = SlugHelper.GenerateSlug(string.IsNullOrWhiteSpace(request.Slug) ? request.Name : request.Slug);
        var exists = await _unitOfWork.Categories.ExistsBySlugAsync(slug, cancellationToken);
        if (exists)
        {
            throw new ValidationAppException("Slug", $"Slug '{slug}' đã tồn tại cho danh mục khác.");
        }

        Category? parent = null;
        if (request.ParentId.HasValue)
        {
            parent = await _unitOfWork.Categories.GetByIdAsync(request.ParentId.Value, cancellationToken);
            if (parent == null) throw new NotFoundException("Parent Category", request.ParentId.Value);

            if (parent.Depth >= 2)
            {
                throw new ValidationAppException("ParentId", "Hệ thống chỉ hỗ trợ tối đa 3 cấp danh mục (Depth: 0, 1, 2). Không thể tạo danh mục con cấp 4.");
            }
        }

        var category = new Category(
            name: request.Name,
            slug: slug,
            parentId: request.ParentId,
            parentPath: parent?.Path,
            parentDepth: parent?.Depth ?? -1,
            displayOrder: request.DisplayOrder,
            productType: request.ProductType,
            moderationPolicy: request.ModerationPolicy,
            commissionRateBp: request.CommissionRateBp);

        await _unitOfWork.Categories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Cache Invalidation
        _cacheService.Remove(CacheKeys.CategoriesTree);
        _logger.LogInformation("Admin tạo danh mục mới: {Name} ({Slug})", category.Name, category.Slug);

        return new CategoryDetailDto
        {
            Id = category.Id,
            ParentId = category.ParentId,
            ParentName = parent?.Name,
            Name = category.Name,
            Slug = category.Slug,
            Path = category.Path,
            Depth = category.Depth,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,
            ProductType = category.ProductType.ToString(),
            ModerationPolicy = category.ModerationPolicy.ToString(),
            CommissionRateBp = category.CommissionRateBp,
            CreatedAtUtc = category.CreatedAtUtc
        };
    }

    public async Task<CategoryDetailDto> UpdateCategoryAsync(Guid id, UpdateCategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
        if (category == null) throw new NotFoundException("Category", id);

        var slug = SlugHelper.GenerateSlug(string.IsNullOrWhiteSpace(request.Slug) ? request.Name : request.Slug);
        if (slug != category.Slug)
        {
            var exists = await _unitOfWork.Categories.ExistsBySlugAsync(slug, cancellationToken);
            if (exists) throw new ValidationAppException("Slug", $"Slug '{slug}' đã tồn tại.");
        }

        category.Update(
            request.Name,
            slug,
            request.DisplayOrder,
            request.ProductType,
            request.ModerationPolicy,
            request.CommissionRateBp);

        category.SetActive(request.IsActive);
        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Cache Invalidation
        _cacheService.Remove(CacheKeys.CategoriesTree);
        _cacheService.Remove(CacheKeys.CategoryAttributes(id));

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
            UpdatedAtUtc = category.UpdatedAtUtc
        };
    }

    public async Task<bool> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
        if (category == null) throw new NotFoundException("Category", id);

        var children = await _unitOfWork.Categories.GetChildrenAsync(id, cancellationToken);
        if (children.Count > 0)
        {
            throw new ValidationAppException("Category", "Không thể xóa danh mục đang có danh mục con. Hãy xóa hoặc chuyển danh mục con trước.");
        }

        _unitOfWork.Categories.Delete(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Cache Invalidation
        _cacheService.Remove(CacheKeys.CategoriesTree);
        _cacheService.Remove(CacheKeys.CategoryAttributes(id));

        return true;
    }

    public async Task<IReadOnlyList<AttributeDefinitionDto>> GetAllAttributesAsync(CancellationToken cancellationToken = default)
    {
        var attributes = await _unitOfWork.Attributes.GetAllAsync(cancellationToken);
        return attributes.Select(MapAttributeToDto).ToList();
    }

    public async Task<AttributeDefinitionDto> CreateAttributeAsync(CreateAttributeRequestDto request, CancellationToken cancellationToken = default)
    {
        var code = SlugHelper.GenerateSlug(request.Code);
        var existing = await _unitOfWork.Attributes.GetByCodeAsync(code, cancellationToken);
        if (existing != null)
        {
            throw new ValidationAppException("Code", $"Mã thuộc tính '{code}' đã tồn tại.");
        }

        var attribute = new AttributeDefinition(
            code: code,
            name: request.Name,
            dataType: request.DataType,
            unit: request.Unit,
            min: request.Min,
            max: request.Max);

        if (request.InitialOptions != null && request.InitialOptions.Count > 0)
        {
            int order = 0;
            foreach (var optValue in request.InitialOptions)
            {
                if (string.IsNullOrWhiteSpace(optValue)) continue;
                var optSlug = SlugHelper.GenerateSlug(optValue);
                attribute.Options.Add(new AttributeOption(attribute.Id, optValue.Trim(), optSlug, order++));
            }
        }

        await _unitOfWork.Attributes.AddAsync(attribute, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapAttributeToDto(attribute);
    }

    public async Task<AttributeDefinitionDto> UpdateAttributeAsync(Guid id, UpdateAttributeRequestDto request, CancellationToken cancellationToken = default)
    {
        var attribute = await _unitOfWork.Attributes.GetByIdAsync(id, cancellationToken);
        if (attribute == null) throw new NotFoundException("Attribute", id);

        attribute.Update(request.Name, request.DataType, request.Unit, request.Min, request.Max);
        _unitOfWork.Attributes.Update(attribute);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapAttributeToDto(attribute);
    }

    public async Task<AttributeOptionDto> AddAttributeOptionAsync(Guid attributeId, CreateAttributeOptionDto request, CancellationToken cancellationToken = default)
    {
        var attribute = await _unitOfWork.Attributes.GetByIdAsync(attributeId, cancellationToken);
        if (attribute == null) throw new NotFoundException("Attribute", attributeId);

        var slug = SlugHelper.GenerateSlug(string.IsNullOrWhiteSpace(request.Slug) ? request.Value : request.Slug);
        var option = new AttributeOption(attributeId, request.Value, slug, request.DisplayOrder);

        await _unitOfWork.Attributes.AddOptionAsync(option, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AttributeOptionDto
        {
            Id = option.Id,
            AttributeId = option.AttributeId,
            Value = option.Value,
            Slug = option.Slug,
            DisplayOrder = option.DisplayOrder,
            IsActive = option.IsActive
        };
    }

    public async Task<bool> DeleteAttributeOptionAsync(Guid optionId, CancellationToken cancellationToken = default)
    {
        var option = await _unitOfWork.Attributes.GetOptionByIdAsync(optionId, cancellationToken);
        if (option == null) throw new NotFoundException("AttributeOption", optionId);

        _unitOfWork.Attributes.DeleteOption(option);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> AssignAttributeToCategoryAsync(Guid categoryId, AssignCategoryAttributeRequestDto request, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(categoryId, cancellationToken);
        if (category == null) throw new NotFoundException("Category", categoryId);

        var attribute = await _unitOfWork.Attributes.GetByIdAsync(request.AttributeId, cancellationToken);
        if (attribute == null) throw new NotFoundException("Attribute", request.AttributeId);

        var existing = await _unitOfWork.Attributes.GetCategoryAttributeAsync(categoryId, request.AttributeId, cancellationToken);
        if (existing != null)
        {
            existing.Update(request.Requirement, request.IsFilterable, request.IsVariantAxis);
        }
        else
        {
            var ca = new CategoryAttribute(categoryId, request.AttributeId, request.Requirement, request.IsFilterable, request.IsVariantAxis);
            await _unitOfWork.Attributes.AssignCategoryAttributeAsync(ca, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Cache Invalidation
        _cacheService.Remove(CacheKeys.CategoryAttributes(categoryId));

        return true;
    }

    public async Task<bool> RemoveAttributeFromCategoryAsync(Guid categoryId, Guid attributeId, CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.Attributes.GetCategoryAttributeAsync(categoryId, attributeId, cancellationToken);
        if (existing == null) throw new NotFoundException("CategoryAttribute link not found.");

        _unitOfWork.Attributes.RemoveCategoryAttribute(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Cache Invalidation
        _cacheService.Remove(CacheKeys.CategoryAttributes(categoryId));

        return true;
    }

    public async Task<(IReadOnlyList<CategoryRequestDto> Items, int Total)> GetCategoryRequestsAsync(
        Domain.Enums.CategoryRequestStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var items = await _unitOfWork.CategoryRequests.GetRequestsAsync(status, page, pageSize, cancellationToken);
        var total = await _unitOfWork.CategoryRequests.GetTotalCountAsync(status, cancellationToken);

        var dtos = items.Select(r => new CategoryRequestDto
        {
            Id = r.Id,
            ShopId = r.ShopId,
            ShopName = r.Shop?.Name ?? string.Empty,
            Name = r.Name,
            SuggestedParentId = r.SuggestedParentId,
            SuggestedParentName = r.SuggestedParent?.Name,
            ReasonNote = r.ReasonNote,
            Status = r.Status.ToString(),
            AdminNote = r.AdminNote,
            ResolvedCategoryId = r.ResolvedCategoryId,
            ResolvedCategoryName = r.ResolvedCategory?.Name,
            CreatedAtUtc = r.CreatedAtUtc,
            ReviewedAtUtc = r.ReviewedAtUtc
        }).ToList();

        return (dtos, total);
    }

    public async Task<CategoryRequestDto> ReviewCategoryRequestAsync(Guid id, ReviewCategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        var adminId = GetCurrentAdminId();
        var catRequest = await _unitOfWork.CategoryRequests.GetByIdAsync(id, cancellationToken);
        if (catRequest == null) throw new NotFoundException("CategoryRequest", id);

        if (request.IsApproved)
        {
            if (!request.ResolvedCategoryId.HasValue)
            {
                throw new ValidationAppException("ResolvedCategoryId", "Khi chấp thuận yêu cầu, bắt buộc phải chọn danh mục được giải quyết/liên kết.");
            }

            catRequest.Approve(adminId, request.ResolvedCategoryId.Value, request.AdminNote);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.AdminNote))
            {
                throw new ValidationAppException("AdminNote", "Từ chối yêu cầu bắt buộc phải có lý do gửi cho người bán.");
            }

            catRequest.Reject(adminId, request.AdminNote);
        }

        _unitOfWork.CategoryRequests.Update(catRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Gửi thông báo đến chủ Shop
        if (catRequest.Shop != null)
        {
            await _notificationService.SendNotificationAsync(new CreateNotificationDto
            {
                UserId = catRequest.Shop.OwnerUserId,
                Type = request.IsApproved ? "category_request_approved" : "category_request_rejected",
                Title = request.IsApproved ? "Đề xuất danh mục được chấp thuận" : "Đề xuất danh mục bị từ chối",
                Body = request.IsApproved
                    ? $"Đề xuất mở danh mục '{catRequest.Name}' của bạn đã được Admin phê duyệt."
                    : $"Đề xuất mở danh mục '{catRequest.Name}' chưa được chấp thuận. Lý do: {request.AdminNote}",
                DataJson = JsonSerializer.Serialize(new { catRequest.Id, catRequest.Name, catRequest.Status })
            }, cancellationToken);
        }

        return new CategoryRequestDto
        {
            Id = catRequest.Id,
            ShopId = catRequest.ShopId,
            ShopName = catRequest.Shop?.Name ?? string.Empty,
            Name = catRequest.Name,
            SuggestedParentId = catRequest.SuggestedParentId,
            SuggestedParentName = catRequest.SuggestedParent?.Name,
            ReasonNote = catRequest.ReasonNote,
            Status = catRequest.Status.ToString(),
            AdminNote = catRequest.AdminNote,
            ResolvedCategoryId = catRequest.ResolvedCategoryId,
            CreatedAtUtc = catRequest.CreatedAtUtc,
            ReviewedAtUtc = catRequest.ReviewedAtUtc
        };
    }

    private Guid GetCurrentAdminId()
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Yêu cầu đăng nhập.");
        }
        return _currentUserService.UserId.Value;
    }

    private static AttributeDefinitionDto MapAttributeToDto(AttributeDefinition def)
    {
        return new AttributeDefinitionDto
        {
            Id = def.Id,
            Code = def.Code,
            Name = def.Name,
            DataType = def.DataType.ToString(),
            Unit = def.Unit,
            Min = def.Min,
            Max = def.Max,
            Options = def.Options.Select(o => new AttributeOptionDto
            {
                Id = o.Id,
                AttributeId = o.AttributeId,
                Value = o.Value,
                Slug = o.Slug,
                DisplayOrder = o.DisplayOrder,
                IsActive = o.IsActive
            }).OrderBy(o => o.DisplayOrder).ToList()
        };
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
}
