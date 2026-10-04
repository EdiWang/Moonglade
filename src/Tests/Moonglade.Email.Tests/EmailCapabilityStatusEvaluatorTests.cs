using Moonglade.Email.Core;

namespace Moonglade.Email.Tests;

public class EmailCapabilityStatusEvaluatorTests
{
    private readonly EmailCapabilityStatusEvaluator _sut = new(
        new EmailServiceOptionsValidator(),
        new EmailOutboxWorkerOptionsValidator());

    [Fact]
    public void Evaluate_RepositoryDefaultSmtpSettings_AreNotConfigured()
    {
        var serviceOptions = new EmailServiceOptions();

        var status = _sut.Evaluate(serviceOptions, new EmailOutboxWorkerOptions());

        Assert.Equal(EmailCapabilityState.NotConfigured, status.State);
        Assert.False(status.IsAvailable);
        Assert.Empty(status.ValidationErrors);
    }

    [Fact]
    public void Evaluate_PartiallyMissingSmtpSettings_AreNotConfigured()
    {
        var serviceOptions = CreateValidSmtpOptions();
        serviceOptions.SmtpPassword = "";

        var status = _sut.Evaluate(serviceOptions, new EmailOutboxWorkerOptions());

        Assert.Equal(EmailCapabilityState.NotConfigured, status.State);
        Assert.Empty(status.ValidationErrors);
    }

    [Fact]
    public void Evaluate_InvalidSmtpPort_IsInvalid()
    {
        var serviceOptions = CreateValidSmtpOptions();
        serviceOptions.SmtpPort = 0;

        var status = _sut.Evaluate(serviceOptions, new EmailOutboxWorkerOptions());

        Assert.Equal(EmailCapabilityState.Invalid, status.State);
        Assert.Contains(status.ValidationErrors, error => error.Contains("Email:SmtpPort", StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_InvalidWorkerSettings_AreInvalid()
    {
        var workerOptions = new EmailOutboxWorkerOptions
        {
            BatchSize = 0
        };

        var status = _sut.Evaluate(CreateValidSmtpOptions(), workerOptions);

        Assert.Equal(EmailCapabilityState.Invalid, status.State);
        Assert.Contains(status.ValidationErrors, error => error.Contains("Email:OutboxWorker:BatchSize", StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_MissingAndInvalidSettings_InvalidTakesPrecedence()
    {
        var serviceOptions = CreateValidSmtpOptions();
        serviceOptions.SmtpPassword = "";
        serviceOptions.SmtpPort = 0;

        var status = _sut.Evaluate(serviceOptions, new EmailOutboxWorkerOptions());

        Assert.Equal(EmailCapabilityState.Invalid, status.State);
        Assert.Single(status.ValidationErrors);
        Assert.Contains("Email:SmtpPort", status.ValidationErrors[0]);
    }

    [Fact]
    public void Evaluate_ValidSmtpWithDisabledWorker_IsDisabled()
    {
        var workerOptions = new EmailOutboxWorkerOptions
        {
            Enabled = false
        };

        var status = _sut.Evaluate(CreateValidSmtpOptions(), workerOptions);

        Assert.Equal(EmailCapabilityState.Disabled, status.State);
        Assert.False(status.IsAvailable);
        Assert.Empty(status.ValidationErrors);
    }

    [Fact]
    public void Evaluate_ValidSmtpSettings_AreAvailable()
    {
        var status = _sut.Evaluate(CreateValidSmtpOptions(), new EmailOutboxWorkerOptions());

        Assert.Equal(EmailCapabilityState.Available, status.State);
        Assert.True(status.IsAvailable);
        Assert.Empty(status.ValidationErrors);
    }

    [Theory]
    [InlineData("notifications@example.com", EmailCapabilityState.Available)]
    [InlineData("not-an-email", EmailCapabilityState.Invalid)]
    public void Evaluate_SmtpWithSeparateSenderAddress_UsesSenderValidation(
        string senderAddress, EmailCapabilityState expectedState)
    {
        var serviceOptions = CreateValidSmtpOptions();
        serviceOptions.SmtpUserName = "resend";
        serviceOptions.SmtpSenderAddress = senderAddress;

        var status = _sut.Evaluate(serviceOptions, new EmailOutboxWorkerOptions());

        Assert.Equal(expectedState, status.State);
    }

    private static EmailServiceOptions CreateValidSmtpOptions() => new()
    {
        SmtpServer = "smtp.example.com",
        SmtpUserName = "sender@example.com",
        SmtpPassword = "password",
        SmtpPort = 587
    };
}
