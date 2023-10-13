using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;

namespace Moonglade.Auth;

public record ChangePasswordCommand(Guid Id, string ClearPassword) : IRequest;

public class ChangePasswordCommandHandler(IRepository<LocalAccountEntity> repo) : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var account = await repo.GetAsync(request.Id, ct);
        if (account is null)
        {
            throw new InvalidOperationException($"LocalAccountEntity with Id '{request.Id}' not found.");
        }

        var newSalt = Helper.GenerateSalt();
        account.PasswordSalt = newSalt;
        account.PasswordHash = Helper.HashPassword2(request.ClearPassword, newSalt);

        await repo.UpdateAsync(account, ct);
    }
}