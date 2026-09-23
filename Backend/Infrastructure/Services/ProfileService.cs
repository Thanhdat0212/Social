using Application.Common;
using Application.DTOs.Profile.Requests;
using Application.DTOs.Profile.Responses;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using AutoMapper;
using Domain.Exceptions;

namespace Infrastructure.Services;

public class ProfileService : IProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAvatarStorageService _avatarStorageService;
    private readonly IMapper _mapper;

    public ProfileService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAvatarStorageService avatarStorageService,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _avatarStorageService = avatarStorageService;
        _mapper = mapper;
    }

    public async Task<ProfileDto> GetCurrentUserProfileAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        return _mapper.Map<ProfileDto>(user);
    }

    public async Task<ProfileDto> UpdateProfileAsync(UpdateProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        user.DisplayName = request.DisplayName.Trim();
        user.Bio = request.Bio?.Trim();
        user.UpdatedAtUtc = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ProfileDto>(user);
    }

    public async Task<string> UploadAvatarAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        var (url, publicId) = await _avatarStorageService.UploadAvatarAsync(userId, fileStream, fileName, cancellationToken);

        user.AvatarUrl = url;
        user.AvatarPublicId = publicId;
        user.UpdatedAtUtc = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return url;
    }

    private Guid GetCurrentUserId()
    {
        return _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("Bạn cần đăng nhập để thực hiện thao tác này.");
    }
}
