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

    public async Task<IReadOnlyList<Post>> FindSimilarPostsByVectorAsync(
        float[] userVector,
        IEnumerable<Guid>? excludePostIds = null,
        int topN = 200,
        CancellationToken cancellationToken = default)
    {
        if (userVector.Length == 0 || topN <= 0)
        {
            return Array.Empty<Post>();
        }

        var excludeSet = excludePostIds != null ? new HashSet<Guid>(excludePostIds) : new HashSet<Guid>();

        // Lấy các bài viết có embedding gần đây (tối đa 500 bài ứng viên)
        var candidateEmbeddings = await _context.PostEmbeddings
            .Include(pe => pe.Post)
                .ThenInclude(p => p.Author)
            .Include(pe => pe.Post)
                .ThenInclude(p => p.PostInterests)
                    .ThenInclude(pi => pi.Interest)
            .Where(pe => !excludeSet.Contains(pe.PostId))
            .OrderByDescending(pe => pe.CreatedAtUtc)
            .Take(500)
            .ToListAsync(cancellationToken);

        if (candidateEmbeddings.Count == 0)
        {
            return Array.Empty<Post>();
        }

        var scored = new List<(Post Post, double Similarity)>();

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
                    scored.Add((pe.Post, sim));
                }
            }
        }

        return scored
            .OrderByDescending(s => s.Similarity)
            .Take(topN)
            .Select(s => s.Post)
            .ToList();
    }
}
