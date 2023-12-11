﻿using Moonglade.Data.Generated.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;

namespace Moonglade.Auth;

public record ValidateLoginCommand(string Username, string InputPassword) : IRequest<Guid>;

public class ValidateLoginCommandHandler(IRepository<LocalAccountEntity> repo) : IRequestHandler<ValidateLoginCommand, Guid>
{
    public async Task<Guid> Handle(ValidateLoginCommand request, CancellationToken ct)
    {
        var account = await repo.GetAsync(p => p.Username == request.Username);
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

            await repo.UpdateAsync(account, ct);
        }

        return valid ? account.Id : Guid.Empty;
    }
}