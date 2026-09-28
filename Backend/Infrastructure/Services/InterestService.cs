using Application.Common;
using Application.DTOs.Interests;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using AutoMapper;
using Domain.Exceptions;

namespace Infrastructure.Services;

public class InterestService : IInterestService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public InterestService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<InterestDto>> GetAllInterestsAsync(CancellationToken cancellationToken = default)
    {
        var interests = await _unitOfWork.Interests.GetAllActiveAsync(cancellationToken);
        var dtos = _mapper.Map<List<InterestDto>>(interests);

        if (_currentUserService.IsAuthenticated && _currentUserService.UserId.HasValue)
        {
            var userInterests = await _unitOfWork.UserInterests.GetByUserIdAsync(_currentUserService.UserId.Value, cancellationToken);
            var selectedIds = userInterests.Select(ui => ui.InterestId).ToHashSet();

            foreach (var dto in dtos)
            {
                dto.IsSelected = selectedIds.Contains(dto.Id);
            }
        }

        return dtos;
    }

    public async Task<IReadOnlyList<InterestDto>> GetMyInterestsAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var userInterests = await _unitOfWork.UserInterests.GetByUserIdAsync(userId, cancellationToken);
        var dtos = userInterests.Select(ui => new InterestDto
        {
            Id = ui.Interest.Id,
            Name = ui.Interest.Name,
            Slug = ui.Interest.Slug,
            Description = ui.Interest.Description,
            Icon = ui.Interest.Icon,
            IsSelected = true
        }).ToList();

        return dtos;
    }

    public async Task<bool> SelectMyInterestsAsync(SelectInterestsRequestDto request, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        // 1. Kiểm tra User tồn tại
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        // 2. Kiểm tra danh sách sở thích hợp lệ
        var distinctIds = request.InterestIds.Distinct().ToList();
        if (distinctIds.Count < 1)
        {
            throw new ValidationAppException("InterestIds", "Vui lòng chọn tối thiểu 1 sở thích để hệ thống gợi ý nội dung phù hợp.");
        }

        var foundInterests = await _unitOfWork.Interests.GetByIdsAsync(distinctIds, cancellationToken);
        if (foundInterests.Count != distinctIds.Count)
        {
            throw new ValidationAppException("InterestIds", "Một hoặc nhiều sở thích được chọn không tồn tại hoặc đã bị vô hiệu hóa.");
        }

        // 3. Thực hiện lưu trữ trong Transaction
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.UserInterests.SetUserInterestsAsync(userId, distinctIds, cancellationToken);
            await _unitOfWork.UserPreferences.InitializePreferencesAsync(userId, distinctIds, initialScore: 1.0, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<UserPreferenceDto>> GetMyPreferencesAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var preferences = await _unitOfWork.UserPreferences.GetByUserIdAsync(userId, cancellationToken);
        return _mapper.Map<List<UserPreferenceDto>>(preferences);
    }

    public async Task<OnboardingStatusDto> GetMyOnboardingStatusAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var userInterests = await _unitOfWork.UserInterests.GetByUserIdAsync(userId, cancellationToken);
        var count = userInterests.Count;

        return new OnboardingStatusDto
        {
            IsOnboarded = count >= 1,
            SelectedCount = count
        };
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");
        }

        return _currentUserService.UserId.Value;
    }
}
