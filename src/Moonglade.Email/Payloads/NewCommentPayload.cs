using System.ComponentModel.DataAnnotations;

namespace Moonglade.Email;

public class NewCommentPayload
{
    [Required]
    [StringLength(256)]
    public string Username { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string Email { get; set; }

    [Required]
    [StringLength(45)]
    public string IpAddress { get; set; }

    [Required]
    [StringLength(512)]
    public string PostTitle { get; set; }

    [Required]
    [StringLength(10000)]
    public string CommentContent { get; set; }
}
