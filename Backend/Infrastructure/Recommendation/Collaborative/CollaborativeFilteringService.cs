using Application.Interfaces.Recommendation;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Recommendation.Collaborative;

public class CollaborativeFilteringService : ICollaborativeFilteringService
{
    private readonly SocialDbContext _context;
    private readonly ILogger<CollaborativeFilteringService> _logger;

    public CollaborativeFilteringService(
        SocialDbContext context,
        ILogger<CollaborativeFilteringService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<(Guid UserId, double Similarity)>> FindSimilarUsersAsync(
        Guid userId,
        int topN = 20,
        CancellationToken cancellationToken = default)
    {
        // 1. Lấy vector sở thích của User mục tiêu
        var myPreferences = await _context.UserPreferences
            .Where(up => up.UserId == userId && up.Score > 0)
            .ToDictionaryAsync(up => up.InterestId, up => up.Score, cancellationToken);

        if (myPreferences.Count == 0)
        {
            return Array.Empty<(Guid UserId, double Similarity)>();
        }

        var myInterestIds = myPreferences.Keys.ToList();
        double normA = Math.Sqrt(myPreferences.Values.Sum(v => v * v));
        if (normA <= 0.0001)
        {
            return Array.Empty<(Guid UserId, double Similarity)>();
        }

        // 2. Lấy danh sách sở thích của các người dùng khác có chung ít nhất 1 chủ đề
        var rawOtherPreferences = await _context.UserPreferences
            .Where(up => up.UserId != userId && myInterestIds.Contains(up.InterestId) && up.Score > 0)
            .Select(up => new { up.UserId, up.InterestId, up.Score })
            .Take(2000)
            .ToListAsync(cancellationToken);

        var otherPreferences = rawOtherPreferences
            .GroupBy(up => up.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Preferences = g.Select(p => new { p.InterestId, p.Score }).ToList()
            })
            .Take(200)
            .ToList();

        if (otherPreferences.Count == 0)
        {
            return Array.Empty<(Guid UserId, double Similarity)>();
        }

        // 3. Tính Cosine Similarity
        var similarities = new List<(Guid UserId, double Similarity)>();

        foreach (var other in otherPreferences)
        {
            double dotProduct = 0.0;
            double sumSquaresB = 0.0;

            foreach (var pref in other.Preferences)
            {
                sumSquaresB += pref.Score * pref.Score;
                if (myPreferences.TryGetValue(pref.InterestId, out var myScore))
                {
                    dotProduct += myScore * pref.Score;
                }
            }

            double normB = Math.Sqrt(sumSquaresB);
            if (normB > 0.0001)
            {
                double similarity = dotProduct / (normA * normB);
                if (similarity >= 0.25)
                {
                    similarities.Add((other.UserId, similarity));
                }
            }
        }

        return similarities
            .OrderByDescending(s => s.Similarity)
            .Take(topN)
            .ToList();
    }

    public async Task<IReadOnlyList<Post>> GetCollaborativeCandidatesAsync(
        Guid userId,
        int targetCount = 200,
        CancellationToken cancellationToken = default)
    {
        if (targetCount <= 0) targetCount = 200;

        // 1. Tìm các người dùng tương tự
        var similarUsers = await FindSimilarUsersAsync(userId, 30, cancellationToken);
        if (similarUsers.Count == 0)
        {
            return Array.Empty<Post>();
        }

        var similarUserMap = similarUsers.ToDictionary(s => s.UserId, s => s.Similarity);
        var similarUserIds = similarUserMap.Keys.ToList();

        // 2. Lấy danh sách bài viết user hiện tại đã tương tác hoặc đã xem để loại trừ
        var myInteractedPostIds = await _context.UserInteractions
            .Where(ui => ui.UserId == userId && ui.PostId.HasValue)
            .Select(ui => ui.PostId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var excludePostIds = new HashSet<Guid>(myInteractedPostIds);

        // 3. Lấy các bài viết mà các người dùng tương đồng đã tương tác tích cực trong 30 ngày gần nhất
        var cutoff = DateTime.UtcNow.AddDays(-30);

        var interactions = await _context.UserInteractions
            .Where(ui => similarUserIds.Contains(ui.UserId)
                         && ui.PostId.HasValue
                         && !excludePostIds.Contains(ui.PostId.Value)
                         && (ui.InteractionType == InteractionType.Like
                             || ui.InteractionType == InteractionType.Save
                             || ui.InteractionType == InteractionType.Comment)
                         && ui.CreatedAtUtc >= cutoff)
            .Select(ui => new
            {
                ui.UserId,
                PostId = ui.PostId!.Value,
                ui.InteractionType
            })
            .ToListAsync(cancellationToken);

        if (interactions.Count == 0)
        {
            return Array.Empty<Post>();
        }

        // 4. Tính điểm bài viết dựa trên trọng số tương đồng và loại tương tác
        var postScoreMap = new Dictionary<Guid, double>();
        foreach (var item in interactions)
        {
            if (!similarUserMap.TryGetValue(item.UserId, out var sim))
            {
                continue;
            }

            double typeWeight = item.InteractionType switch
            {
                InteractionType.Save => 2.0,
                InteractionType.Comment => 1.5,
                InteractionType.Like => 1.0,
                _ => 0.5
            };

            var score = sim * typeWeight;
            if (postScoreMap.ContainsKey(item.PostId))
            {
                postScoreMap[item.PostId] += score;
            }
            else
            {
                postScoreMap[item.PostId] = score;
            }
        }

        // Lấy top targetCount IDs
        var topPostIds = postScoreMap
            .OrderByDescending(kv => kv.Value)
            .Take(targetCount)
            .Select(kv => kv.Key)
            .ToList();

        if (topPostIds.Count == 0)
        {
            return Array.Empty<Post>();
        }

        // 5. Truy vấn danh sách Post chi tiết
        var posts = await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.PostInterests)
                .ThenInclude(pi => pi.Interest)
            .Where(p => topPostIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        // Giữ thứ tự theo điểm số
        var orderMap = topPostIds
            .Select((id, index) => new { id, index })
            .ToDictionary(x => x.id, x => x.index);

        return posts.OrderBy(p => orderMap.TryGetValue(p.Id, out var idx) ? idx : int.MaxValue).ToList();
    }
}
