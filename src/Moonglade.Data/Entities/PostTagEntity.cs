namespace Moonglade.Data.Entities;

public class PostTagEntity
{
    public Guid PostId { get; set; }
    public int TagId { get; set; }

    public PostEntity Post { get; set; }
    public TagEntity Tag { get; set; }
}