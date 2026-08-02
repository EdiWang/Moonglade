namespace Moonglade.Email.Core;

public class EmailServiceOptions
{
    public const string SmtpProvider = "smtp";
    public const string AzureCommunicationProvider = "azurecommunication";

    public string SmtpServer { get; set; } = string.Empty;
    public string SmtpUserName { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 25;
    public bool EnableSsl { get; set; }
    public string SenderDisplayName { get; set; } = string.Empty;
    public string Provider { get; set; } = SmtpProvider;
    public string AcsConnectionString { get; set; } = string.Empty;
    public string AcsSenderAddress { get; set; } = string.Empty;

    public string NormalizedProvider => NormalizeProvider(Provider);

    public static string NormalizeProvider(string provider) =>
        string.IsNullOrWhiteSpace(provider) ? SmtpProvider : provider.Trim().ToLowerInvariant();
}
