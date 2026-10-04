using Microsoft.Extensions.Options;
using Moonglade.Email.Core;

namespace Moonglade.Email.Tests;

public class EmailServiceOptionsValidatorTests
{
    private readonly EmailServiceOptionsValidator _sut = new();

    [Fact]
    public void Validate_SmtpWithRequiredSettings_Succeeds()
    {
        var options = new EmailServiceOptions
        {
            Provider = "smtp",
            SmtpServer = "smtp.example.com",
            SmtpUserName = "user@example.com",
            SmtpPassword = "password",
            SmtpPort = 587
        };

        var result = _sut.Validate(Options.DefaultName, options);

        Assert.False(result.Failed);
    }

    [Fact]
    public void Validate_BlankProviderUsesSmtpRules()
    {
        var options = new EmailServiceOptions { Provider = "" };

        var result = _sut.Validate(Options.DefaultName, options);

        Assert.True(result.Failed);
        Assert.Contains("Email:SmtpServer", result.FailureMessage);
    }

    [Theory]
    [InlineData("notifications@example.com", true)]
    [InlineData("", true)]
    [InlineData(null, true)]
    [InlineData("   ", true)]
    [InlineData("not-an-email", false)]
    [InlineData("Blog <notifications@example.com>", false)]
    [InlineData("notifications@example.com\r\nBcc: other@example.com", false)]
    public void Validate_SmtpSenderAddress_IsOptionalAndValidatedWhenProvided(string senderAddress, bool valid)
    {
        var options = new EmailServiceOptions
        {
            SmtpServer = "smtp.example.com",
            SmtpUserName = "smtp-login",
            SmtpPassword = "do-not-log-this-password",
            SmtpSenderAddress = senderAddress
        };

        var result = _sut.Validate(Options.DefaultName, options);

        Assert.Equal(valid, result.Succeeded);
        if (!valid)
        {
            Assert.Contains("Email:SmtpSenderAddress", result.FailureMessage);
            Assert.DoesNotContain(options.SmtpPassword, result.FailureMessage);
        }
    }

    [Fact]
    public void Validate_AzureCommunication_IgnoresSmtpSenderAddress()
    {
        var options = new EmailServiceOptions
        {
            Provider = "AzureCommunication",
            AcsConnectionString = "endpoint=https://example.communication.azure.com/;accesskey=fake",
            AcsSenderAddress = "no-reply@example.com",
            SmtpSenderAddress = "not-an-email"
        };

        Assert.True(_sut.Validate(Options.DefaultName, options).Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    public void Validate_InvalidSmtpPort_Fails(int port)
    {
        var options = new EmailServiceOptions
        {
            Provider = "smtp",
            SmtpServer = "smtp.example.com",
            SmtpUserName = "user@example.com",
            SmtpPassword = "password",
            SmtpPort = port
        };

        var result = _sut.Validate(Options.DefaultName, options);

        Assert.True(result.Failed);
        Assert.Contains("Email:SmtpPort", result.FailureMessage);
    }

    [Fact]
    public void Validate_AzureCommunicationWithRequiredSettings_Succeeds()
    {
        var options = new EmailServiceOptions
        {
            Provider = "AzureCommunication",
            AcsConnectionString = "endpoint=https://example.communication.azure.com/;accesskey=fake",
            AcsSenderAddress = "no-reply@example.com"
        };

        var result = _sut.Validate(Options.DefaultName, options);

        Assert.False(result.Failed);
    }

    [Fact]
    public void Validate_AzureCommunicationMissingConnectionString_Fails()
    {
        var options = new EmailServiceOptions
        {
            Provider = "AzureCommunication",
            AcsSenderAddress = "no-reply@example.com"
        };

        var result = _sut.Validate(Options.DefaultName, options);

        Assert.True(result.Failed);
        Assert.Contains("Email:AcsConnectionString", result.FailureMessage);
    }

    [Fact]
    public void Validate_AzureCommunicationInvalidSenderAddress_Fails()
    {
        var options = new EmailServiceOptions
        {
            Provider = "AzureCommunication",
            AcsConnectionString = "endpoint=https://example.communication.azure.com/;accesskey=fake",
            AcsSenderAddress = "not-an-email"
        };

        var result = _sut.Validate(Options.DefaultName, options);

        Assert.True(result.Failed);
        Assert.Contains("Email:AcsSenderAddress", result.FailureMessage);
    }

    [Fact]
    public void Validate_UnsupportedProvider_Fails()
    {
        var options = new EmailServiceOptions { Provider = "sendgrid" };

        var result = _sut.Validate(Options.DefaultName, options);

        Assert.True(result.Failed);
        Assert.Contains("Email provider", result.FailureMessage);
    }
}
