using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Moonglade.Web.Models
{
    public class FriendLinkEditModel
    {
        [HiddenInput]
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Title")]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Link")]
        [DataType(DataType.Url)]
        public string LinkUrl { get; set; }

        public FriendLinkEditModel()
        {
            Id = Guid.Empty;
        }
    }
}
