using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Configuration;

public class LocalAccountRateLimitOptions
{
    public const string SectionName = "Authentication:LocalAccountRateLimit";

    public bool Enabled { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; } = 10;

    [Range(1, int.MaxValue)]
    public int WindowMinutes { get; set; } = 1;
}
