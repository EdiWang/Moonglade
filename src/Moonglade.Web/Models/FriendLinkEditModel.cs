using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models
{
    public class FriendLinkEditModel
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
