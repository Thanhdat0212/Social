using System.Threading.Channels;
using Application.Interfaces;

namespace Infrastructure.AI;

public class PostAiChannel : IPostAiChannel
{
    private readonly Channel<Guid> _channel;

    public PostAiChannel()
    {
        var options = new BoundedChannelOptions(5000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<Guid>(options);
    }

    public async ValueTask WriteAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(postId, cancellationToken);
    }

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
