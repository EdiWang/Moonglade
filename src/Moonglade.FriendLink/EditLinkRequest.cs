using System.ComponentModel.DataAnnotations;

namespace Moonglade.FriendLink
{
    public class EditLinkRequest
    {
        [Required]
        [Display(Name = "Title")]
        [MaxLength(64)]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Link")]
        [DataType(DataType.Url)]
        [MaxLength(256)]
        public string LinkUrl { get; set; }
    }
}
