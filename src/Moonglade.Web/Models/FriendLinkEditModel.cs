using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Moonglade.Web.Models
{
    public class FriendLinkEditModel
    {
        [HiddenInput]
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Title")]
        [MaxLength(64)]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Link")]
        [DataType(DataType.Url)]
        [MaxLength(256)]
        public string LinkUrl { get; set; }

        public FriendLinkEditModel()
        {
            Id = Guid.Empty;
        }
    }
}
