using Application.Common;
using Application.DTOs.Posts;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PostService : IPostService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPostAiChannel _postAiChannel;
    private readonly IMapper _mapper;
    private readonly ILogger<PostService> _logger;

    public PostService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IPostAiChannel postAiChannel,
        IMapper mapper,
        ILogger<PostService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _postAiChannel = postAiChannel;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PostDto> CreatePostAsync(CreatePostRequestDto request, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("Người dùng không tồn tại.");
        }

        var post = new Post
        {
            AuthorId = currentUserId,
            Content = request.Content?.Trim() ?? string.Empty,
            MediaUrls = request.MediaUrls?.Where(url => !string.IsNullOrWhiteSpace(url)).Select(url => url.Trim()).ToList() ?? new List<string>(),
            Status = PostStatus.Published,
            LikeCount = 0,
            CommentCount = 0,
            ViewCount = 0,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _unitOfWork.Posts.AddAsync(post, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Đẩy PostId vào kênh bất đồng bộ để Gemini AI phân tích nội dung ngầm
        if (!string.IsNullOrWhiteSpace(post.Content))
        {
            try
            {
                await _postAiChannel.WriteAsync(post.Id, cancellationToken);
                _logger.LogInformation("Đã đẩy bài viết {PostId} vào hàng đợi phân tích AI.", post.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể đẩy bài viết {PostId} vào hàng đợi AI.", post.Id);
            }
        }

        var postDto = _mapper.Map<PostDto>(post);
        postDto.Author = _mapper.Map<PostAuthorDto>(user);
        postDto.Topics = new List<PostTopicDto>();
        postDto.IsLikedByCurrentUser = false;

        return postDto;
    }

    public async Task<IReadOnlyList<PostDto>> GetRecentPostsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 20;

        var posts = await _unitOfWork.Posts.GetRecentPostsAsync(page, pageSize, cancellationToken);
        var postDtos = _mapper.Map<List<PostDto>>(posts);

        if (_currentUserService.IsAuthenticated && _currentUserService.UserId.HasValue)
        {
            var likedPostIds = (await _unitOfWork.PostLikes.GetLikedPostIdsByUserAsync(
                _currentUserService.UserId.Value,
                postDtos.Select(p => p.Id),
                cancellationToken)).ToHashSet();

            foreach (var dto in postDtos)
            {
                dto.IsLikedByCurrentUser = likedPostIds.Contains(dto.Id);
            }
        }

        return postDtos;
    }

    public async Task<PostDto> GetPostByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var post = await _unitOfWork.Posts.GetWithDetailsByIdAsync(id, cancellationToken);
        if (post == null)
        {
            throw new NotFoundException("Bài viết không tồn tại hoặc đã bị xóa.");
        }

        var postDto = _mapper.Map<PostDto>(post);

        if (_currentUserService.IsAuthenticated && _currentUserService.UserId.HasValue)
        {
            postDto.IsLikedByCurrentUser = await _unitOfWork.PostLikes.IsLikedAsync(
                _currentUserService.UserId.Value, post.Id, cancellationToken);
        }

        return postDto;
    }

    public async Task DeletePostAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        var post = await _unitOfWork.Posts.GetByIdAsync(id, cancellationToken);
        if (post == null)
        {
            throw new NotFoundException("Bài viết không tồn tại hoặc đã bị xóa.");
        }

        if (post.AuthorId != currentUserId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xóa bài viết này.");
        }

        _unitOfWork.Posts.Delete(post);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Người dùng {UserId} đã xóa bài viết {PostId}.", currentUserId, id);
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Yêu cầu xác thực tài khoản.");
        }

        return _currentUserService.UserId.Value;
    }
}
