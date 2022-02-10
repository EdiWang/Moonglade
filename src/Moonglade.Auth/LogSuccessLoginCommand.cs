using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Auth;

public record LogSuccessLoginCommand(Guid Id, string IpAddress) : IRequest;

public class LogSuccessLoginCommandHandler : IRequestHandler<LogSuccessLoginCommand>
{
    private readonly IRepository<LocalAccountEntity> _accountRepo;

    public LogSuccessLoginCommandHandler(IRepository<LocalAccountEntity> accountRepo)
    {
        _accountRepo = accountRepo;
    }

    public async Task<Unit> Handle(LogSuccessLoginCommand request, CancellationToken cancellationToken)
    {
        var entity = await _accountRepo.GetAsync(request.Id);
        if (entity is not null)
        {
            entity.LastLoginIp = request.IpAddress.Trim();
            entity.LastLoginTimeUtc = DateTime.UtcNow;
            await _accountRepo.UpdateAsync(entity);
        }

        return Unit.Value;
    }
}