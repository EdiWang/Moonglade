namespace Moonglade.Auth;

public class AzureAdOption
{
    public string ClientId { get; set; }

    public string Instance { get; set; }

    public string Domain { get; set; }

    public string TenantId { get; set; }

    public string CallbackPath { get; set; }
}