using System.ComponentModel.DataAnnotations;

namespace Moonglade.Email;

public class CommentReplyPayload
{
    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string Email { get; set; }

    [Required]
    [StringLength(10000)]
    public string CommentContent { get; set; }

    [Required]
    [StringLength(512)]
    public string Title { get; set; }

    [Required]
    [StringLength(20000)]
    public string ReplyContentHtml { get; set; }

    [Required]
    [Url]
    [StringLength(2048)]
    public string PostLink { get; set; }
}
