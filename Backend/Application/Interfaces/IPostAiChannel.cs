namespace Application.Interfaces;

public interface IPostAiChannel
{
    ValueTask WriteAsync(Guid postId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken = default);
}
