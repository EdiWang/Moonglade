using System.ComponentModel.DataAnnotations;

namespace Moonglade.Features.Comment;

public class CommentRequest
{
    [Required]
    [MaxLength(64)]
    public string Username { get; set; }

    [Required]
    [DataType(DataType.MultilineText), MaxLength(1024)]
    public string Content { get; set; }

    [MaxLength(128)]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; }

    [MaxLength(128)]
    public string Source { get; set; }

    public long? FormRenderedUtc { get; set; }
}
