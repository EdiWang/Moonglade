using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Auth;

public record LogSuccessLoginCommand(string IpAddress) : IRequest;

public class LogSuccessLoginCommandHandler(IRepository<LoginHistoryEntity> repo) : IRequestHandler<LogSuccessLoginCommand>
{
    public async Task Handle(LogSuccessLoginCommand request, CancellationToken ct)
    {
        var entity = new LoginHistoryEntity
        {
            LoginIp = request.IpAddress.Trim(),
            LoginTimeUtc = DateTime.UtcNow
        };

        await repo.AddAsync(entity, ct);
    }
}