﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Moonglade.Data.Entities;

[Table("CustomPage")]
public partial class CustomPageEntity
{
    [Key]
    public Guid Id { get; set; }

    [StringLength(128)]
    public string Title { get; set; }

    [StringLength(128)]
    public string Slug { get; set; }

    [StringLength(256)]
    public string MetaDescription { get; set; }

    public string HtmlContent { get; set; }

    public string CssContent { get; set; }

    public bool HideSidebar { get; set; }

    public bool IsPublished { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreateTimeUtc { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdateTimeUtc { get; set; }
}