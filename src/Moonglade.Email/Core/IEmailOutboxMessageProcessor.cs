namespace Moonglade.Email.Core;

public interface IEmailOutboxMessageProcessor
{
    Task<bool> ProcessNextAsync(string workerId, CancellationToken cancellationToken = default);
}
