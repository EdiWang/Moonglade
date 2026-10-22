using Moonglade.Email.Core;

namespace Moonglade.Web.Pages.Admin.Settings;

public sealed class EmailNotificationPageState
{
    private EmailNotificationPageState(
        EmailCapabilityStatus capabilityStatus,
        string senderAddress,
        bool emailEnabled,
        bool emailSendingEnabled)
    {
        CapabilityState = capabilityStatus.State;
        ValidationErrors = capabilityStatus.ValidationErrors;
        SenderAddress = senderAddress;
        EmailEnabled = emailEnabled;
        CanSendTestEmail = capabilityStatus.IsAvailable && emailSendingEnabled;
    }

    public EmailCapabilityState CapabilityState { get; }

    public IReadOnlyList<string> ValidationErrors { get; }

    public string ProviderLabel => "SMTP";

    public string SenderAddress { get; }

    public bool EmailEnabled { get; }

    public bool CanSendTestEmail { get; }

    public bool ShowNotConfiguredPrompt => CapabilityState == EmailCapabilityState.NotConfigured;

    public bool ShowInvalidConfiguration => CapabilityState == EmailCapabilityState.Invalid;

    public bool ShowDisabledNotice => CapabilityState == EmailCapabilityState.Disabled;

    public static EmailNotificationPageState Create(
        EmailCapabilityStatus capabilityStatus,
        EmailServiceOptions serviceOptions,
        bool emailSendingEnabled)
    {
        ArgumentNullException.ThrowIfNull(capabilityStatus);
        ArgumentNullException.ThrowIfNull(serviceOptions);

        return new(
            capabilityStatus,
            serviceOptions.EffectiveSmtpSenderAddress,
            serviceOptions.Enabled,
            emailSendingEnabled);
    }
}
