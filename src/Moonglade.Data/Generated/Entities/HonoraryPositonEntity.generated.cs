//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
//
//     Produced by Entity Framework Visual Editor v4.2.5.1
//     Source:                    https://github.com/msawczyn/EFDesigner
//     Visual Studio Marketplace: https://marketplace.visualstudio.com/items?itemName=michaelsawczyn.EFDesigner
//     Documentation:             https://msawczyn.github.io/EFDesigner/
//     License (MIT):             https://github.com/msawczyn/EFDesigner/blob/master/LICENSE
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Moonglade.Data
{
   /// <summary>
   /// Table for our Honorary Positions
   /// </summary>
   [System.ComponentModel.Description("Table for our Honorary Positions")]
   public partial class HonoraryPositonEntity
   {
      partial void Init();

      /// <summary>
      /// Default constructor. Protected due to required properties, but present because EF needs it.
      /// </summary>
      protected HonoraryPositonEntity()
      {
         Init();
      }

      /// <summary>
      /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
      /// </summary>
      public static HonoraryPositonEntity CreateHonoraryPositonEntityUnsafe()
      {
         return new HonoraryPositonEntity();
      }

      /// <summary>
      /// Public constructor with required data
      /// </summary>
      /// <param name="link">Link to the website what contains the honorary position</param>
      /// <param name="organization">The Organization where you have that position</param>
      /// <param name="summary">Summary of the work there</param>
      /// <param name="active">Is it active?</param>
      public HonoraryPositonEntity(string link, string organization, string summary, bool active)
      {
         if (string.IsNullOrEmpty(link)) throw new ArgumentNullException(nameof(link));
         this.Link = link;

         if (string.IsNullOrEmpty(organization)) throw new ArgumentNullException(nameof(organization));
         this.Organization = organization;

         if (string.IsNullOrEmpty(summary)) throw new ArgumentNullException(nameof(summary));
         this.Summary = summary;

         this.Active = active;

         Init();
      }

      /// <summary>
      /// Static create function (for use in LINQ queries, etc.)
      /// </summary>
      /// <param name="link">Link to the website what contains the honorary position</param>
      /// <param name="organization">The Organization where you have that position</param>
      /// <param name="summary">Summary of the work there</param>
      /// <param name="active">Is it active?</param>
      public static HonoraryPositonEntity Create(string link, string organization, string summary, bool active)
      {
         return new HonoraryPositonEntity(link, organization, summary, active);
      }

      /*************************************************************************
       * Properties
       *************************************************************************/

      /// <summary>
      /// Required
      /// Is it active?
      /// </summary>
      [Required]
      [System.ComponentModel.Description("Is it active?")]
      public bool Active { get; set; }

      /// <summary>
      /// Identity, Required
      /// Identification Automatically increased
      /// </summary>
      [Key]
      [Required]
      [System.ComponentModel.Description("Identification Automatically increased")]
      public int Id { get; set; }

      /// <summary>
      /// The language
      /// </summary>
      [System.ComponentModel.Description("The language")]
      public global::Moonglade.Data.Enum.LanguageEnum? Language { get; set; }

      /// <summary>
      /// Required, Max length = 150
      /// Link to the website what contains the honorary position
      /// </summary>
      [Required]
      [MaxLength(150)]
      [StringLength(150)]
      [System.ComponentModel.Description("Link to the website what contains the honorary position")]
      public string Link { get; set; }

      /// <summary>
      /// Required, Max length = 100
      /// The Organization where you have that position
      /// </summary>
      [Required]
      [MaxLength(100)]
      [StringLength(100)]
      [System.ComponentModel.Description("The Organization where you have that position")]
      public string Organization { get; set; }

      /// <summary>
      /// Required, Max length = 200
      /// Summary of the work there
      /// </summary>
      [Required]
      [MaxLength(200)]
      [StringLength(200)]
      [System.ComponentModel.Description("Summary of the work there")]
      public string Summary { get; set; }

   }
}

