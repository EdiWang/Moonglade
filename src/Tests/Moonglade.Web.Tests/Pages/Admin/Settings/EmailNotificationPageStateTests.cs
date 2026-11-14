using Moonglade.Email.Core;
using Moonglade.Web.Pages.Admin.Settings;

namespace Moonglade.Web.Tests.Pages.Admin.Settings;

public class EmailNotificationPageStateTests
{
    [Theory]
    [InlineData(EmailCapabilityState.Available, true)]
    [InlineData(EmailCapabilityState.NotConfigured, false)]
    [InlineData(EmailCapabilityState.Invalid, false)]
    [InlineData(EmailCapabilityState.Disabled, false)]
    public void Create_MapsCapabilityStateToTestEmailAvailability(
        EmailCapabilityState expectedState,
        bool canSendTestEmail)
    {
        var (serviceOptions, workerOptions) = CreateOptions(expectedState);
        var capabilityStatus = CreateStatus(serviceOptions, workerOptions);

        var pageState = EmailNotificationPageState.Create(
            capabilityStatus,
            serviceOptions,
            emailSendingEnabled: true);

        Assert.Equal(expectedState, pageState.CapabilityState);
        Assert.Equal(serviceOptions.Enabled, pageState.EmailEnabled);
        Assert.Equal(canSendTestEmail, pageState.CanSendTestEmail);

        if (expectedState == EmailCapabilityState.NotConfigured)
        {
            Assert.Empty(pageState.ValidationErrors);
        }

        if (expectedState == EmailCapabilityState.Invalid)
        {
            Assert.NotEmpty(pageState.ValidationErrors);
        }
    }

    [Fact]
    public void Create_WhenBlogEmailSendingIsDisabled_PreventsTestEmail()
    {
        var (serviceOptions, workerOptions) = CreateOptions(EmailCapabilityState.Available);
        var capabilityStatus = CreateStatus(serviceOptions, workerOptions);

        var pageState = EmailNotificationPageState.Create(
            capabilityStatus,
            serviceOptions,
            emailSendingEnabled: false);

        Assert.False(pageState.CanSendTestEmail);
    }

    [Theory]
    [InlineData("notifications@example.com", "notifications@example.com")]
    [InlineData("", "sender@example.com")]
    [InlineData(null, "sender@example.com")]
    [InlineData("   ", "sender@example.com")]
    public void Create_SmtpSenderAddress_UsesEffectiveSender(string? configuredAddress, string expectedAddress)
    {
        var (serviceOptions, workerOptions) = CreateOptions(EmailCapabilityState.Available);
        serviceOptions.SmtpSenderAddress = configuredAddress;

        var pageState = EmailNotificationPageState.Create(
            CreateStatus(serviceOptions, workerOptions), serviceOptions, emailSendingEnabled: true);

        Assert.Equal(expectedAddress, pageState.SenderAddress);
        Assert.True(pageState.CanSendTestEmail);
    }

    [Fact]
    public void Create_InvalidSmtpSenderAddress_DisablesTestEmail()
    {
        var (serviceOptions, workerOptions) = CreateOptions(EmailCapabilityState.Available);
        serviceOptions.SmtpSenderAddress = "not-an-email";

        var pageState = EmailNotificationPageState.Create(
            CreateStatus(serviceOptions, workerOptions), serviceOptions, emailSendingEnabled: true);

        Assert.False(pageState.CanSendTestEmail);
        Assert.Contains(pageState.ValidationErrors, error => error.Contains("Email:SmtpSenderAddress", StringComparison.Ordinal));
    }

    private static EmailCapabilityStatus CreateStatus(
        EmailServiceOptions serviceOptions,
        EmailOutboxWorkerOptions workerOptions) =>
        new EmailCapabilityStatusEvaluator(
            new EmailServiceOptionsValidator(),
            new EmailOutboxWorkerOptionsValidator())
        .Evaluate(serviceOptions, workerOptions);

    private static (EmailServiceOptions ServiceOptions, EmailOutboxWorkerOptions WorkerOptions) CreateOptions(
        EmailCapabilityState state)
    {
        var serviceOptions = new EmailServiceOptions
        {
            SmtpServer = "smtp.example.com",
            SmtpUserName = "sender@example.com",
            SmtpPassword = "test-password",
            SmtpPort = 587
        };
        var workerOptions = new EmailOutboxWorkerOptions();

        switch (state)
        {
            case EmailCapabilityState.NotConfigured:
                serviceOptions.SmtpPassword = string.Empty;
                break;
            case EmailCapabilityState.Invalid:
                serviceOptions.SmtpPort = 0;
                break;
            case EmailCapabilityState.Disabled:
                serviceOptions.Enabled = false;
                break;
        }

        return (serviceOptions, workerOptions);
    }
}
