using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Auth;

public record LogSuccessLoginCommand(Guid Id, string IpAddress) : IRequest;

public class LogSuccessLoginCommandHandler : IRequestHandler<LogSuccessLoginCommand>
{
    private readonly IRepository<LocalAccountEntity> _repo;
    public LogSuccessLoginCommandHandler(IRepository<LocalAccountEntity> repo) => _repo = repo;

    public async Task Handle(LogSuccessLoginCommand request, CancellationToken ct)
    {
        var (id, ipAddress) = request;

        var entity = await _repo.GetAsync(id, ct);
        if (entity is not null)
        {
            entity.LastLoginIp = ipAddress.Trim();
            entity.LastLoginTimeUtc = DateTime.UtcNow;
            await _repo.UpdateAsync(entity, ct);
        }
    }
}