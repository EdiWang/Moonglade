namespace Moonglade.Email.Core;

public class EmailServiceOptions
{
    public bool Enabled { get; set; } = true;
    public string SmtpServer { get; set; } = string.Empty;
    public string SmtpUserName { get; set; } = string.Empty;
    public string SmtpSenderAddress { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 25;
    public bool EnableSsl { get; set; }
    public string SenderDisplayName { get; set; } = string.Empty;

    public string EffectiveSmtpSenderAddress =>
        string.IsNullOrWhiteSpace(SmtpSenderAddress) ? SmtpUserName : SmtpSenderAddress;
}
