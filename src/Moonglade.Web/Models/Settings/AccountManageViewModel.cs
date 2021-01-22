using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Moonglade.Core;

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
        [Required(ErrorMessage = "Please enter a username.")]
        [Display(Name = "Username")]
        [MinLength(2, ErrorMessage = "Username must be at least 2 characters"), MaxLength(32)]
        [RegularExpression("[a-z0-9]+", ErrorMessage = "Username must be lower case letters or numbers.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Please enter a password.")]
        [Display(Name = "Password")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters"), MaxLength(32)]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$", ErrorMessage = "Password must be minimum eight characters, at least one letter and one number")]
        public string Password { get; set; }
    }
}
