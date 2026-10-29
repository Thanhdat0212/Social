using Application.DTOs.Recommendation;
using Application.Interfaces.Recommendation;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Recommendation.Semantic;

public class SemanticVectorSearchService : ISemanticVectorSearchService
{
    private readonly SocialDbContext _context;
    private readonly ILogger<SemanticVectorSearchService> _logger;

    public SemanticVectorSearchService(
        SocialDbContext context,
        ILogger<SemanticVectorSearchService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<float[]?> BuildUserInterestVectorAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);

        // Lấy các bài viết người dùng đã tương tác tích cực gần đây có sẵn Embedding
        var interactions = await _context.UserInteractions
            .AsNoTracking()
            .Include(ui => ui.Post)
                .ThenInclude(p => p!.Embedding)
            .Where(ui => ui.UserId == userId
                         && ui.PostId.HasValue
                         && ui.Post != null
                         && ui.Post.Embedding != null
                         && ui.CreatedAtUtc >= cutoff)
            .OrderByDescending(ui => ui.CreatedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (interactions.Count == 0)
        {
            return null;
        }

        int dimension = 0;
        foreach (var item in interactions)
        {
            var emb = item.Post?.Embedding?.Values;
            if (emb != null && emb.Length > 0)
            {
                dimension = emb.Length;
                break;
            }
        }

        if (dimension == 0)
        {
            return null;
        }

        var accumulator = new double[dimension];
        double totalWeight = 0.0;

        foreach (var item in interactions)
        {
            var emb = item.Post?.Embedding?.Values;
            if (emb == null || emb.Length != dimension) continue;

            double weight = item.InteractionType switch
            {
                InteractionType.Save => 2.0,
                InteractionType.Comment => 1.5,
                InteractionType.Like => 1.0,
                InteractionType.View => 0.2,
                _ => 0.5
            };

            for (int i = 0; i < dimension; i++)
            {
                accumulator[i] += emb[i] * weight;
            }

            totalWeight += weight;
        }

        if (totalWeight <= 0.0001)
        {
            return null;
        }

        // Chuẩn hoá vector (L2 norm)
        double normSquared = 0.0;
        for (int i = 0; i < dimension; i++)
        {
            accumulator[i] /= totalWeight;
            normSquared += accumulator[i] * accumulator[i];
        }

        double norm = Math.Sqrt(normSquared);
        if (norm <= 0.00001)
        {
            return null;
        }

        var resultVector = new float[dimension];
        for (int i = 0; i < dimension; i++)
        {
            resultVector[i] = (float)(accumulator[i] / norm);
        }

        return resultVector;
    }

    public async Task<IReadOnlyList<PostCandidateDto>> FindSimilarPostsByVectorAsync(
        float[] userVector,
        IEnumerable<Guid>? excludePostIds = null,
        int topN = 200,
        CancellationToken cancellationToken = default)
    {
        if (userVector.Length == 0 || topN <= 0)
        {
            return Array.Empty<PostCandidateDto>();
        }

        var excludeSet = excludePostIds != null ? new HashSet<Guid>(excludePostIds) : new HashSet<Guid>();

        // Lấy các bài viết có embedding gần đây (chỉ lấy PostId và Values, 1 câu SQL cực nhẹ)
        var candidateEmbeddings = await _context.PostEmbeddings
            .AsNoTracking()
            .Where(pe => !excludeSet.Contains(pe.PostId))
            .OrderByDescending(pe => pe.CreatedAtUtc)
            .Take(500)
            .Select(pe => new { pe.PostId, pe.Values })
            .ToListAsync(cancellationToken);

        if (candidateEmbeddings.Count == 0)
        {
            return Array.Empty<PostCandidateDto>();
        }

        var scored = new List<(Guid PostId, double Similarity)>();

        foreach (var pe in candidateEmbeddings)
        {
            var values = pe.Values;
            if (values.Length != userVector.Length) continue;

            // Tính Cosine Similarity
            double dot = 0.0;
            double normPostSq = 0.0;

            for (int i = 0; i < userVector.Length; i++)
            {
                dot += userVector[i] * values[i];
                normPostSq += values[i] * values[i];
            }

            double normPost = Math.Sqrt(normPostSq);
            if (normPost > 0.0001)
            {
                double sim = dot / normPost;
                if (sim >= 0.4) // Ngưỡng tương đồng ngữ nghĩa
                {
                    scored.Add((pe.PostId, sim));
                }
            }
        }

        var topPostIds = scored
            .OrderByDescending(s => s.Similarity)
            .Take(topN)
            .Select(s => s.PostId)
            .ToList();

        if (topPostIds.Count == 0)
        {
            return Array.Empty<PostCandidateDto>();
        }

        // Truy vấn DTO siêu nhẹ cho topPostIds
        var candidates = await _context.Posts
            .AsNoTracking()
            .Where(p => topPostIds.Contains(p.Id) && p.Status == PostStatus.Published)
            .Select(p => new PostCandidateDto
            {
                Id = p.Id,
                AuthorId = p.AuthorId,
                CreatedAtUtc = p.CreatedAtUtc,
                LikeCount = p.LikeCount,
                CommentCount = p.CommentCount,
                ViewCount = p.ViewCount,
                PostInterests = p.PostInterests.Select(pi => new CandidatePostInterestDto
                {
                    InterestId = pi.InterestId,
                    Name = pi.Interest.Name,
                    Confidence = pi.Confidence
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var orderMap = topPostIds.Select((id, index) => new { id, index }).ToDictionary(x => x.id, x => x.index);
        return candidates.OrderBy(p => orderMap.TryGetValue(p.Id, out var idx) ? idx : int.MaxValue).ToList();
    }
}
