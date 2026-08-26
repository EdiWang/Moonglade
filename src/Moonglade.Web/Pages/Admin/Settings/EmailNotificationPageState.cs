using Moonglade.Email.Core;

namespace Moonglade.Web.Pages.Admin.Settings;

public sealed class EmailNotificationPageState
{
    private EmailNotificationPageState(
        EmailCapabilityStatus capabilityStatus,
        string providerLabel,
        string senderAddress,
        bool workerEnabled,
        bool emailSendingEnabled)
    {
        CapabilityState = capabilityStatus.State;
        ValidationErrors = capabilityStatus.ValidationErrors;
        ProviderLabel = providerLabel;
        SenderAddress = senderAddress;
        WorkerEnabled = workerEnabled;
        CanSendTestEmail = capabilityStatus.IsAvailable && emailSendingEnabled;
    }

    public EmailCapabilityState CapabilityState { get; }

    public IReadOnlyList<string> ValidationErrors { get; }

    public string ProviderLabel { get; }

    public string SenderAddress { get; }

    public bool WorkerEnabled { get; }

    public bool CanSendTestEmail { get; }

    public bool ShowNotConfiguredPrompt => CapabilityState == EmailCapabilityState.NotConfigured;

    public bool ShowInvalidConfiguration => CapabilityState == EmailCapabilityState.Invalid;

    public bool ShowDisabledNotice => CapabilityState == EmailCapabilityState.Disabled;

    public static EmailNotificationPageState Create(
        EmailCapabilityStatus capabilityStatus,
        EmailServiceOptions serviceOptions,
        EmailOutboxWorkerOptions workerOptions,
        bool emailSendingEnabled)
    {
        ArgumentNullException.ThrowIfNull(capabilityStatus);
        ArgumentNullException.ThrowIfNull(serviceOptions);
        ArgumentNullException.ThrowIfNull(workerOptions);

        var providerLabel = serviceOptions.NormalizedProvider switch
        {
            EmailServiceOptions.AzureCommunicationProvider => "Azure Communication Services",
            EmailServiceOptions.SmtpProvider => "SMTP",
            _ => serviceOptions.Provider.Trim()
        };

        var senderAddress = serviceOptions.NormalizedProvider switch
        {
            EmailServiceOptions.AzureCommunicationProvider => serviceOptions.AcsSenderAddress,
            EmailServiceOptions.SmtpProvider => serviceOptions.SmtpUserName,
            _ => string.Empty
        };

        return new(
            capabilityStatus,
            providerLabel,
            senderAddress,
            workerOptions.Enabled,
            emailSendingEnabled);
    }
}
