using LiteBus.Commands.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Auth;

public record LogSuccessLoginCommand(string IpAddress, string UserAgent) : ICommand;

public class LogSuccessLoginCommandHandler(MoongladeRepository<LoginHistoryEntity> repo) : ICommandHandler<LogSuccessLoginCommand>
{
    public async Task HandleAsync(LogSuccessLoginCommand request, CancellationToken ct)
    {
        var entity = new LoginHistoryEntity
        {
            LoginIp = request.IpAddress.Trim(),
            LoginTimeUtc = DateTime.UtcNow,
            LoginUserAgent = request.UserAgent.Trim(),
        };

        await repo.AddAsync(entity, ct);
    }
}