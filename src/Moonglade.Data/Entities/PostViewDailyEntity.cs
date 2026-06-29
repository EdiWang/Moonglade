namespace Moonglade.Data.Entities;

public class PostViewDailyEntity
{
    public Guid PostId { get; set; }

    public DateTime ViewDateUtc { get; set; }

    public int ViewCount { get; set; }
}
