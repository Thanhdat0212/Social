namespace Application.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IVerificationTokenRepository VerificationTokens { get; }
    IInterestRepository Interests { get; }
    IUserInterestRepository UserInterests { get; }
    IUserPreferenceRepository UserPreferences { get; }
    IPostRepository Posts { get; }
    IPostInterestRepository PostInterests { get; }
    IPostLikeRepository PostLikes { get; }
    ICommentRepository Comments { get; }
    IUserFollowRepository UserFollows { get; }
    IUserInteractionRepository UserInteractions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
