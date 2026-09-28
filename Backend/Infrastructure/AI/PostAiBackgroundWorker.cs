using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.AI;

public class PostAiBackgroundWorker : BackgroundService
{
    private readonly IPostAiChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PostAiBackgroundWorker> _logger;

    public PostAiBackgroundWorker(
        IPostAiChannel channel,
        IServiceScopeFactory scopeFactory,
        ILogger<PostAiBackgroundWorker> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PostAiBackgroundWorker đã khởi động và sẵn sàng xử lý bài viết.");

        await foreach (var postId in _channel.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessPostAsync(postId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi xử lý phân loại AI ngầm cho bài viết {PostId}.", postId);
            }
        }

        _logger.LogInformation("PostAiBackgroundWorker đang dừng hoạt động.");
    }

    private async Task ProcessPostAsync(Guid postId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var aiAnalyzer = scope.ServiceProvider.GetRequiredService<IAiContentAnalyzer>();

        var post = await unitOfWork.Posts.GetByIdAsync(postId, cancellationToken);
        if (post == null)
        {
            _logger.LogWarning("Không tìm thấy bài viết {PostId} để phân tích AI.", postId);
            return;
        }

        if (string.IsNullOrWhiteSpace(post.Content))
        {
            return;
        }

        // Kiểm tra xem bài viết đã được gắn topic trước đó chưa
        var existing = await unitOfWork.PostInterests.GetByPostIdAsync(postId, cancellationToken);
        if (existing.Count > 0)
        {
            return;
        }

        // Lấy danh sách tên các chủ đề đang kích hoạt
        var allInterests = await unitOfWork.Interests.GetAllActiveAsync(cancellationToken);
        if (allInterests.Count == 0)
        {
            _logger.LogWarning("Không có chủ đề nào trong hệ thống để phân loại.");
            return;
        }

        var candidateTopicNames = allInterests.Select(i => i.Name).ToList();

        // Gọi Gemini phân tích
        var classificationResults = await aiAnalyzer.AnalyzeContentTopicsAsync(
            post.Content,
            candidateTopicNames,
            cancellationToken);

        if (classificationResults.Count == 0)
        {
            _logger.LogInformation("Không có chủ đề nào vượt qua ngưỡng tin cậy cho bài viết {PostId}.", postId);
            return;
        }

        var interestMap = allInterests.ToDictionary(
            i => i.Name.Trim().ToLowerInvariant(),
            i => i.Id);

        var postInterests = new List<PostInterest>();
        foreach (var result in classificationResults)
        {
            if (interestMap.TryGetValue(result.TopicName.Trim().ToLowerInvariant(), out var interestId))
            {
                postInterests.Add(new PostInterest
                {
                    PostId = post.Id,
                    InterestId = interestId,
                    Confidence = Math.Clamp(result.Confidence, 0.0, 1.0),
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        if (postInterests.Count > 0)
        {
            await unitOfWork.PostInterests.AddRangeAsync(postInterests, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Đã phân loại thành công {Count} chủ đề cho bài viết {PostId}: {Topics}",
                postInterests.Count,
                post.Id,
                string.Join(", ", classificationResults.Select(r => $"{r.TopicName} ({r.Confidence:P0})")));
        }
    }
}
