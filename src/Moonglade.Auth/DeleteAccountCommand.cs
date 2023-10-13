using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Auth;

public record DeleteAccountCommand(Guid Id) : IRequest;

public class DeleteAccountCommandHandler(IRepository<LocalAccountEntity> repo) : IRequestHandler<DeleteAccountCommand>
{
    public async Task Handle(DeleteAccountCommand request, CancellationToken ct)
    {
        var account = await repo.GetAsync(request.Id, ct);
        if (account != null) await repo.DeleteAsync(request.Id, ct);
    }
}