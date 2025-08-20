﻿namespace Moonglade.Data.Entities;

public class CommentEntity
{
    public CommentEntity()
    {
        Replies = new HashSet<CommentReplyEntity>();
    }

    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string IPAddress { get; set; }
    public DateTime CreateTimeUtc { get; set; }
    public string CommentContent { get; set; }
    public Guid PostId { get; set; }
    public bool IsApproved { get; set; }

    public PostEntity Post { get; set; }
    public ICollection<CommentReplyEntity> Replies { get; set; }
}