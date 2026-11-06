using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Auth;

public sealed class UpdateLocalAccountCommand : ICommand
{
    public string Json { get; }

    public UpdateLocalAccountCommand(LocalAccountSettings account)
    {
        ArgumentNullException.ThrowIfNull(account);
        Json = account.ToJson();
    }
}

public class UpdateLocalAccountCommandHandler(BlogDbContext db, LocalAccountState accounts,
    ConfigurationWriteGate gate, IEventMediator events, ILogger<UpdateLocalAccountCommandHandler> logger)
    : ICommandHandler<UpdateLocalAccountCommand>
{
    public async Task HandleAsync(UpdateLocalAccountCommand request, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            var account = request.Json.FromJson<LocalAccountSettings>();
            Validator.ValidateObject(account, new ValidationContext(account), true);
            var key = nameof(LocalAccountSettings);
            var row = await db.BlogConfiguration.SingleOrDefaultAsync(row => row.CfgKey == key, ct)
                ?? throw new InvalidOperationException($"Configuration '{key}' was not found.");
            var changed = row.CfgValue != request.Json;
            if (changed)
            {
                row.CfgValue = request.Json;
                row.LastModifiedTimeUtc = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(ct);
            accounts.Publish(account);
            if (changed) await events.PublishAsync(new ConfigurationChangedEvent([key]), CancellationToken.None);
            logger.LogInformation("Local account settings updated successfully");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to update local account settings");
            throw;
        }
        finally
        {
            gate.Release();
        }
    }
}
