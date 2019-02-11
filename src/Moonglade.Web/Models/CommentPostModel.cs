using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Moonglade.Web.Models
{
    public class CommentPostModel
    {
        [HiddenInput]
        public Guid PostId { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.MultilineText), MaxLength(512)]
        public string Content { get; set; }

        [Required]
        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(4)]
        public string CaptchaCode { get; set; }
    }
}