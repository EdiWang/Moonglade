using System.ComponentModel.DataAnnotations;

namespace Moonglade.Webmention;

public class WebmentionSourceRateLimitOptions
{
    public bool Enabled { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; } = 10;

    [Range(1, int.MaxValue)]
    public int WindowMinutes { get; set; } = 10;
}
