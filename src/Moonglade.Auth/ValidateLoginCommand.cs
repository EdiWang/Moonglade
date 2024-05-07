using Moonglade.Configuration;
using Moonglade.Utils;

namespace Moonglade.Auth;

public record ValidateLoginCommand(string Username, string InputPassword) : IRequest<bool>;

public class ValidateLoginCommandHandler(IBlogConfig config) : IRequestHandler<ValidateLoginCommand, bool>
{
    public Task<bool> Handle(ValidateLoginCommand request, CancellationToken ct)
    {
        var account = config.LocalAccountSettings;

        if (account is null) return Task.FromResult(false);
        if (account.Username != request.Username) return Task.FromResult(false);

        var valid = account.PasswordHash == Helper.HashPassword(request.InputPassword.Trim(), account.PasswordSalt);

        return Task.FromResult(valid);
    }
}