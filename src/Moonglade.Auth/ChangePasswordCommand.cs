using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;

namespace Moonglade.Auth;

public record ChangePasswordCommand(Guid Id, string ClearPassword) : IRequest;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IRepository<LocalAccountEntity> _repo;
    public ChangePasswordCommandHandler(IRepository<LocalAccountEntity> repo) => _repo = repo;

    public async Task Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var account = await _repo.GetAsync(request.Id, ct);
        if (account is null)
        {
            throw new InvalidOperationException($"LocalAccountEntity with Id '{request.Id}' not found.");
        }

        var newSalt = Helper.GenerateSalt();
        account.PasswordSalt = newSalt;
        account.PasswordHash = Helper.HashPassword2(request.ClearPassword, newSalt);

        await _repo.UpdateAsync(account, ct);
    }
}