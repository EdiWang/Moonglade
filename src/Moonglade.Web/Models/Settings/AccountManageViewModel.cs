using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Moonglade.Model;

namespace Moonglade.Web.Models.Settings
{
    public class AccountManageViewModel
    {
        public AccountEditViewModel AccountEditViewModel { get; set; }

        public IReadOnlyList<Account> Accounts { get; set; }

        public AccountManageViewModel()
        {
            Accounts = new List<Account>();
        }
    }

    public class AccountEditViewModel
    {
        [Required]
        [Display(Name = "Username")]
        [MinLength(2), MaxLength(32)]
        [RegularExpression("[a-z0-9]+")]
        public string Username { get; set; }

        [Required]
        [Display(Name = "Password")]
        [MinLength(8), MaxLength(32)]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$", ErrorMessage = "Password must be minimum eight characters, at least one letter and one number")]
        public string Password { get; set; }
    }
}
