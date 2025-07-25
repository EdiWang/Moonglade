using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Utils;

namespace Moonglade.Auth;

public record ValidateLoginCommand(string Username, string InputPassword) : ICommand<bool>;

public class ValidateLoginCommandHandler(
    IBlogConfig config,
    ILogger<ValidateLoginCommandHandler> logger
    ) : ICommandHandler<ValidateLoginCommand, bool>
{
    public Task<bool> HandleAsync(ValidateLoginCommand request, CancellationToken ct)
    {
        var account = config.LocalAccountSettings;

        if (account is null) return Task.FromResult(false);
        if (account.Username != request.Username) return Task.FromResult(false);

        var valid = account.PasswordHash == Helper.HashPassword(request.InputPassword.Trim(), account.PasswordSalt);

        if (!valid)
        {
            logger.LogWarning("Login failed for user {Username}", request.Username);
        }

        return Task.FromResult(valid);
    }
}