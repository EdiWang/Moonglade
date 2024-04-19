using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Moonglade.Data.Entities;

public class LoginHistoryEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public DateTime LoginTimeUtc { get; set; }

    [MaxLength(64)]
    public string LoginIp { get; set; }

    [MaxLength(128)]
    public string LoginUserAgent { get; set; }

    [MaxLength(128)]
    public string DeviceFingerprint { get; set; }
}