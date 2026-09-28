using Application.Interfaces.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly SocialDbContext _context;
    private IDbContextTransaction? _currentTransaction;

    private IUserRepository? _users;
    private IRefreshTokenRepository? _refreshTokens;
    private IVerificationTokenRepository? _verificationTokens;
    private IInterestRepository? _interests;
    private IUserInterestRepository? _userInterests;
    private IUserPreferenceRepository? _userPreferences;
    private IPostRepository? _posts;
    private IPostInterestRepository? _postInterests;
    private IPostLikeRepository? _postLikes;
    private ICommentRepository? _comments;
    private IUserFollowRepository? _userFollows;
    private IUserInteractionRepository? _userInteractions;

    public UnitOfWork(SocialDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(_context);
    public IVerificationTokenRepository VerificationTokens => _verificationTokens ??= new VerificationTokenRepository(_context);
    public IInterestRepository Interests => _interests ??= new InterestRepository(_context);
    public IUserInterestRepository UserInterests => _userInterests ??= new UserInterestRepository(_context);
    public IUserPreferenceRepository UserPreferences => _userPreferences ??= new UserPreferenceRepository(_context);
    public IPostRepository Posts => _posts ??= new PostRepository(_context);
    public IPostInterestRepository PostInterests => _postInterests ??= new PostInterestRepository(_context);
    public IPostLikeRepository PostLikes => _postLikes ??= new PostLikeRepository(_context);
    public ICommentRepository Comments => _comments ??= new CommentRepository(_context);
    public IUserFollowRepository UserFollows => _userFollows ??= new UserFollowRepository(_context);
    public IUserInteractionRepository UserInteractions => _userInteractions ??= new UserInteractionRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            return;
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
            }
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
