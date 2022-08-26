using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;

namespace Moonglade.Auth;

public record ValidateLoginCommand(string Username, string InputPassword) : IRequest<Guid>;

public class ValidateLoginCommandHandler : IRequestHandler<ValidateLoginCommand, Guid>
{
    private readonly IRepository<LocalAccountEntity> _accountRepo;

    public ValidateLoginCommandHandler(IRepository<LocalAccountEntity> accountRepo) => _accountRepo = accountRepo;

    public async Task<Guid> Handle(ValidateLoginCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new ArgumentNullException(nameof(request.Username), "value must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(request.InputPassword))
        {
            throw new ArgumentNullException(nameof(request.InputPassword), "value must not be empty.");
        }

        var account = await _accountRepo.GetAsync(p => p.Username == request.Username);
        if (account is null) return Guid.Empty;

        var valid = account.PasswordHash == (string.IsNullOrWhiteSpace(account.PasswordSalt)
            ? Helper.HashPassword(request.InputPassword.Trim())
            : Helper.HashPassword2(request.InputPassword.Trim(), account.PasswordSalt));

        // migrate old account to salt
        if (valid && string.IsNullOrWhiteSpace(account.PasswordSalt))
        {
            var salt = Helper.GenerateSalt();
            var newHash = Helper.HashPassword2(request.InputPassword.Trim(), salt);

            account.PasswordSalt = salt;
            account.PasswordHash = newHash;

            await _accountRepo.UpdateAsync(account, cancellationToken);
        }

        return valid ? account.Id : Guid.Empty;
    }
}