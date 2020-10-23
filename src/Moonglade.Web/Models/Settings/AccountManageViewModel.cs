using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
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
        [HiddenInput]
        public Guid Id { get; set; }

        [Display(Name = "New Password")]
        [MaxLength(32)]
        public string NewPassword { get; set; }
    }
}
