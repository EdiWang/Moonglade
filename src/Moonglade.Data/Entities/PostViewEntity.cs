using System.ComponentModel.DataAnnotations;

namespace Moonglade.Data.Entities;

public class PostViewEntity
{
    [Key]
    public Guid PostId { get; set; }

    public int RequestCount { get; set; }

    public int ViewCount { get; set; }

    public DateTime BeginTimeUtc { get; set; }
}
