using System.ComponentModel.DataAnnotations;

namespace Moonglade.Auth;

public interface ILocalAccountReader
{
    LocalAccountSettings Read();
}

public sealed class LocalAccountState : ILocalAccountReader
{
    private LocalAccountSettings _current = new();

    public LocalAccountSettings Read() => Copy(Volatile.Read(ref _current));

    public void Publish(LocalAccountSettings account)
    {
        var snapshot = Copy(account);
        Validator.ValidateObject(snapshot, new ValidationContext(snapshot), true);
        Volatile.Write(ref _current, snapshot);
    }

    private static LocalAccountSettings Copy(LocalAccountSettings account) => new()
    {
        Username = account.Username,
        PasswordHash = account.PasswordHash,
        PasswordSalt = account.PasswordSalt,
        TotpSecret = account.TotpSecret,
        IsTotpEnabled = account.IsTotpEnabled
    };
}
