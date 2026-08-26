using Moonglade.Email.Core;
using Moonglade.Web.Pages.Admin.Settings;
using System.Xml.Linq;

namespace Moonglade.Web.Tests;

public class EmailNotificationPageStateTests
{
    private const string SetupPromptResourceKey =
        "Email notifications are not configured. Add email provider settings to enable this feature.";

    private const string InvalidConfigurationResourceKey =
        "Email notification configuration is invalid.";

    [Theory]
    [InlineData(EmailCapabilityState.Available, false, false, false, true)]
    [InlineData(EmailCapabilityState.NotConfigured, true, false, false, false)]
    [InlineData(EmailCapabilityState.Invalid, false, true, false, false)]
    [InlineData(EmailCapabilityState.Disabled, false, false, true, false)]
    public void Create_MapsCapabilityStateToPagePresentation(
        EmailCapabilityState expectedState,
        bool showNotConfiguredPrompt,
        bool showInvalidConfiguration,
        bool showDisabledNotice,
        bool canSendTestEmail)
    {
        var (serviceOptions, workerOptions) = CreateOptions(expectedState);
        var capabilityStatus = CreateStatus(serviceOptions, workerOptions);

        var pageState = EmailNotificationPageState.Create(
            capabilityStatus,
            serviceOptions,
            workerOptions,
            emailSendingEnabled: true);

        Assert.Equal(expectedState, pageState.CapabilityState);
        Assert.Equal(showNotConfiguredPrompt, pageState.ShowNotConfiguredPrompt);
        Assert.Equal(showInvalidConfiguration, pageState.ShowInvalidConfiguration);
        Assert.Equal(showDisabledNotice, pageState.ShowDisabledNotice);
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
            workerOptions,
            emailSendingEnabled: false);

        Assert.False(pageState.CanSendTestEmail);
    }

    [Theory]
    [InlineData("Program.zh-Hans.resx")]
    [InlineData("Program.zh-Hant.resx")]
    [InlineData("Program.de-DE.resx")]
    [InlineData("Program.ja-JP.resx")]
    public void NotificationCapabilityResourceKeys_AreLocalized(string resourceFileName)
    {
        var resourcePath = Path.Combine(
            FindWebProjectRoot(),
            "Resources",
            resourceFileName);
        var document = XDocument.Load(resourcePath);
        var resourceKeys = document
            .Root!
            .Elements("data")
            .Select(element => (string?)element.Attribute("name"))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains(SetupPromptResourceKey, resourceKeys);
        Assert.Contains(InvalidConfigurationResourceKey, resourceKeys);
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
            Provider = EmailServiceOptions.SmtpProvider,
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
                workerOptions.Enabled = false;
                break;
        }

        return (serviceOptions, workerOptions);
    }

    private static string FindWebProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "Moonglade.Web");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate src/Moonglade.Web.");
    }
}
