using System.ComponentModel.DataAnnotations;

namespace Moonglade.Email;

public class PingPayload
{
    [Required]
    [StringLength(512)]
    public string TargetPostTitle { get; set; }

    [Required]
    [StringLength(253)]
    public string Domain { get; set; }

    [Required]
    [StringLength(45)]
    public string SourceIp { get; set; }

    [Required]
    [Url]
    [StringLength(2048)]
    public string SourceUrl { get; set; }

    [Required]
    [StringLength(512)]
    public string SourceTitle { get; set; }
}
