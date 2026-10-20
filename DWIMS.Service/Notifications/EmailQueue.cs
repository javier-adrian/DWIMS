using System.Threading.Channels;

namespace DWIMS.Service.Common;

public sealed class EmailQueue
{
    private readonly Channel<EmailJob> _channel = Channel.CreateUnbounded<EmailJob>();
    
    public void Enqueue(EmailJob job) => _channel.Writer.TryWrite(job);
    
    public IAsyncEnumerable<EmailJob> DequeueAllAsync(CancellationToken cancellationToken) => 
        _channel.Reader.ReadAllAsync(cancellationToken);
}