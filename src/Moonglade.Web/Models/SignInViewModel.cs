using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Moonglade.Web.Models
{
    public class SignInViewModel
    {
        [Required]
        [Display(Name = "Username")]
        [MaxLength(32)]
        public string Username { get; set; }

        [Required]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        [MaxLength(32)]
        public string Password { get; set; }
    }
}
