using System.ComponentModel.DataAnnotations;

namespace Moonglade.Data.Entities;

public class MentionEntity
{
    public Guid Id { get; set; }

    [MaxLength(256)]
    public string Domain { get; set; }

    [MaxLength(256)]
    public string SourceUrl { get; set; }

    [MaxLength(256)]
    public string SourceTitle { get; set; }

    [MaxLength(64)]
    public string SourceIp { get; set; }

    public Guid TargetPostId { get; set; }
    public DateTime PingTimeUtc { get; set; }

    [MaxLength(100)]
    public string TargetPostTitle { get; set; }
}