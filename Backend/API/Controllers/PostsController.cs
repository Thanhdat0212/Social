using Application.Common;
using Application.DTOs.Comments;
using Application.DTOs.Likes;
using Application.DTOs.Posts;
using Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly IFeedService _feedService;
    private readonly IPostMediaStorageService _mediaStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILikeService _likeService;
    private readonly ICommentService _commentService;
    private readonly IValidator<CreatePostRequestDto> _createValidator;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private const long MaxFileSizeInBytes = 10 * 1024 * 1024; // 10MB cho ảnh bài viết

    public PostsController(
        IPostService postService,
        IFeedService feedService,
        IPostMediaStorageService mediaStorageService,
        ICurrentUserService currentUserService,
        ILikeService likeService,
        ICommentService commentService,
        IValidator<CreatePostRequestDto> createValidator)
    {
        _postService = postService;
        _feedService = feedService;
        _mediaStorageService = mediaStorageService;
        _currentUserService = currentUserService;
        _likeService = likeService;
        _commentService = commentService;
        _createValidator = createValidator;
    }

    /// <summary>
    /// Đăng bài viết mới (kèm ảnh tuỳ chọn). Hệ thống sẽ tự động kích hoạt Gemini AI phân tích nội dung ngầm.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreatePost(
        [FromBody] CreatePostRequestDto request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var post = await _postService.CreatePostAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPostById), new { id = post.Id }, post);
    }

    /// <summary>
    /// Bảng tin gợi ý thông minh (For You Feed - 70% Cá nhân hóa, 20% Mở rộng, 10% Khám phá mới)
    /// </summary>
    [HttpGet("feed")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<PostDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForYouFeed(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var feed = await _feedService.GetForYouFeedAsync(page, pageSize, cancellationToken);
        return Ok(feed);
    }

    /// <summary>
    /// Bảng tin từ các tài khoản mà người dùng đang theo dõi (Following Feed)
    /// </summary>
    [HttpGet("following")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<PostDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetFollowingFeed(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var feed = await _feedService.GetFollowingFeedAsync(page, pageSize, cancellationToken);
        return Ok(feed);
    }

    /// <summary>
    /// Lấy danh sách các bài viết mới nhất (phân trang theo thời gian)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<PostDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentPosts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var posts = await _postService.GetRecentPostsAsync(page, pageSize, cancellationToken);
        return Ok(posts);
    }

    /// <summary>
    /// Xem chi tiết một bài viết kèm thông tin tác giả và các chủ đề do AI phân loại
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPostById(Guid id, CancellationToken cancellationToken)
    {
        var post = await _postService.GetPostByIdAsync(id, cancellationToken);
        return Ok(post);
    }

    /// <summary>
    /// Xóa bài viết (chỉ tác giả bài viết mới có quyền xóa)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePost(Guid id, CancellationToken cancellationToken)
    {
        await _postService.DeletePostAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Tải lên hình ảnh cho bài viết lên CDN Cloudinary (Tối đa 10MB, JPG/PNG/WebP/GIF)
    /// </summary>
    [HttpPost("media")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(PostMediaUploadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadMedia(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Tệp không hợp lệ",
                Detail = "Vui lòng chọn tệp ảnh để tải lên."
            });
        }

        if (file.Length > MaxFileSizeInBytes)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Tệp quá dung lượng cho phép",
                Detail = "Kích thước ảnh không được vượt quá 10MB."
            });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Định dạng tệp không được hỗ trợ",
                Detail = "Vui lòng chọn ảnh định dạng JPG, PNG, WebP hoặc GIF."
            });
        }

        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        await using var stream = file.OpenReadStream();
        var (url, publicId) = await _mediaStorageService.UploadPostMediaAsync(
            userId.Value,
            stream,
            file.FileName,
            cancellationToken);

        return Ok(new PostMediaUploadResponseDto
        {
            Url = url,
            PublicId = publicId
        });
    }

    /// <summary>
    /// Thích hoặc bỏ thích một bài viết (Toggle Like)
    /// </summary>
    [HttpPost("{id:guid}/like")]
    [Authorize]
    [ProducesResponseType(typeof(LikeToggleResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleLike(Guid id, CancellationToken cancellationToken)
    {
        var result = await _likeService.ToggleLikeAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách bình luận của bài viết (bao gồm các phản hồi con)
    /// </summary>
    [HttpGet("{id:guid}/comments")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<CommentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPostComments(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var comments = await _commentService.GetPostCommentsAsync(id, page, pageSize, cancellationToken);
        return Ok(comments);
    }

    /// <summary>
    /// Thêm bình luận vào bài viết (hỗ trợ bình luận trực tiếp hoặc trả lời bình luận khác)
    /// </summary>
    [HttpPost("{id:guid}/comments")]
    [Authorize]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateComment(
        Guid id,
        [FromBody] CreateCommentRequestDto request,
        CancellationToken cancellationToken)
    {
        var comment = await _commentService.CreateCommentAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetPostComments), new { id }, comment);
    }
}
