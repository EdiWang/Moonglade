using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Moonglade.Web.Models
{
    public class CategoryEditViewModel
    {
        [HiddenInput]
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Display Name")]
        [MaxLength(64)]
        public string DisplayName { get; set; }

        [Required]
        [Display(Name = "Route Name")]
        [RegularExpression("(?!-)([a-z0-9-]+)", ErrorMessage = "Route Name can only accept lower case English letters (a-z) and numbers (0-9) with/out hyphen (-) in middle.")]
        [MaxLength(64)]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Description")]
        [MaxLength(128)]
        public string Note { get; set; }

        public CategoryEditViewModel()
        {
            Id = Guid.Empty;
        }
    }
}
