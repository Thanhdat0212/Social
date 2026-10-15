using Application.Common;
using Application.DTOs.Marketplace.Categories;
using Application.Interfaces;
using Application.Interfaces.Marketplace.Categories;
using Application.Interfaces.Repositories;
using Domain.Entities.Marketplace.Categories;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Categories;

public class SellerCategoryService : ISellerCategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SellerCategoryService> _logger;

    public SellerCategoryService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<SellerCategoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<CategoryRequestDto> RequestCategoryAsync(CreateCategoryRequestFromSellerDto request, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(currentUserId, cancellationToken);
        if (shop == null)
        {
            throw new ValidationAppException("Shop", "Chỉ chủ gian hàng mới có quyền gửi đề xuất mở danh mục mới.");
        }

        Category? suggestedParent = null;
        if (request.SuggestedParentId.HasValue)
        {
            suggestedParent = await _unitOfWork.Categories.GetByIdAsync(request.SuggestedParentId.Value, cancellationToken);
        }

        var catRequest = new CategoryRequest(
            shopId: shop.Id,
            name: request.Name,
            suggestedParentId: request.SuggestedParentId,
            reasonNote: request.ReasonNote);

        await _unitOfWork.CategoryRequests.AddAsync(catRequest, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Shop {ShopId} ({ShopName}) vừa đề xuất mở danh mục: {Name}", shop.Id, shop.Name, request.Name);

        return new CategoryRequestDto
        {
            Id = catRequest.Id,
            ShopId = shop.Id,
            ShopName = shop.Name,
            Name = catRequest.Name,
            SuggestedParentId = catRequest.SuggestedParentId,
            SuggestedParentName = suggestedParent?.Name,
            ReasonNote = catRequest.ReasonNote,
            Status = catRequest.Status.ToString(),
            CreatedAtUtc = catRequest.CreatedAtUtc
        };
    }

    public async Task<IReadOnlyList<CategoryRequestDto>> GetMyCategoryRequestsAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(currentUserId, cancellationToken);
        if (shop == null) return Array.Empty<CategoryRequestDto>();

        var requests = await _unitOfWork.CategoryRequests.GetByShopIdAsync(shop.Id, cancellationToken);

        return requests.Select(r => new CategoryRequestDto
        {
            Id = r.Id,
            ShopId = r.ShopId,
            ShopName = shop.Name,
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
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Yêu cầu đăng nhập.");
        }
        return _currentUserService.UserId.Value;
    }
}
