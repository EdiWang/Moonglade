using Moonglade.Configuration;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Auth.Tests;

public class LocalAccountStateTests
{
    public static TheoryData<string> InvalidStoredAccounts => new()
    {
        null!, "not-json", "null", "{}", "[]",
        """{"username":null}""",
        Invalid(account => account.Username = ""),
        Invalid(account => account.PasswordHash = ""),
        Invalid(account => account.PasswordSalt = ""),
        Invalid(account => account.IsTotpEnabled = true),
        Invalid(account => account.TotpSecret = "private-marker!")
    };

    [Theory]
    [MemberData(nameof(InvalidStoredAccounts))]
    public void ParseStored_InvalidAccount_ThrowsKeyOnlyError(string json)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => LocalAccountSettings.ParseStored(json));

        Assert.Contains(nameof(LocalAccountSettings), exception.Message);
        Assert.DoesNotContain("private-marker", exception.ToString());
        Assert.Null(exception.InnerException);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ParseStored_LegacyUsernameAndSetupSecret_ArePreserved(bool enabled)
    {
        var account = LocalAccountSettings.DefaultValue;
        account.Username = "legacy.user@example.com";
        account.IsTotpEnabled = enabled;
        account.TotpSecret = " jbsw y3dpehpk3pxp ";
        var json = account.ToJson();

        Assert.Equal(json, LocalAccountSettings.ParseStored(json).ToJson());
    }

    [Fact]
    public void PublishAndRead_DoNotExposeMutablePublishedCredentials()
    {
        var state = new LocalAccountState();
        Assert.Null(state.Read().Username);
        var account = LocalAccountSettings.DefaultValue;
        var original = account.ToJson();

        state.Publish(account);
        account.PasswordHash = "changed-input";
        var read = state.Read();
        read.PasswordSalt = "changed-output";

        Assert.Equal(original, state.Read().ToJson());
        Assert.Throws<ValidationException>(() => state.Publish(new LocalAccountSettings()));
        Assert.Equal(original, state.Read().ToJson());
    }

    [Fact]
    public void Command_CapturesCredentialsWithoutGeneratedDiagnosticString()
    {
        var account = LocalAccountSettings.DefaultValue;
        account.TotpSecret = "JBSWY3DPEHPK3PXP";
        var original = account.ToJson();
        var command = new UpdateLocalAccountCommand(account);

        account.Username = "changed-after-command";

        Assert.Equal(original, command.Json);
        Assert.DoesNotContain(account.PasswordHash, command.ToString());
        Assert.DoesNotContain(account.PasswordSalt, command.ToString());
        Assert.DoesNotContain(account.TotpSecret, command.ToString());
        Assert.DoesNotContain("PasswordHash", account.ToString());
    }

    private static string Invalid(Action<LocalAccountSettings> change)
    {
        var account = LocalAccountSettings.DefaultValue;
        change(account);
        return account.ToJson();
    }
}
