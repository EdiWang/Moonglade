using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Configuration;

public class CommentRateLimitOptions
{
    public const string SectionName = "CommentRateLimit";

    public bool Enabled { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; } = 5;

    [Range(1, int.MaxValue)]
    public int WindowMinutes { get; set; } = 10;
}
