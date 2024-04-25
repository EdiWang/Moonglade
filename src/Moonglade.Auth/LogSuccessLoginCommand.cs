using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Auth;

public record LogSuccessLoginCommand(string IpAddress, string UserAgent, string DeviceFingerprint) : IRequest;

public class LogSuccessLoginCommandHandler(MoongladeRepository<LoginHistoryEntity> repo) : IRequestHandler<LogSuccessLoginCommand>
{
    public async Task Handle(LogSuccessLoginCommand request, CancellationToken ct)
    {
        var entity = new LoginHistoryEntity
        {
            LoginIp = request.IpAddress.Trim(),
            LoginTimeUtc = DateTime.UtcNow,
            LoginUserAgent = request.UserAgent.Trim(),
            DeviceFingerprint = request.DeviceFingerprint.Trim()
        };

        await repo.AddAsync(entity, ct);
    }
}