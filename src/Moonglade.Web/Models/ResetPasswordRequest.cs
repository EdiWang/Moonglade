using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Moonglade.Web.Models
{
    public class ResetPasswordRequest
    {
        [Required]
        public string NewPassword { get; set; }
    }
}
