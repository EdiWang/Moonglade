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
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Moonglade.Data.Context
{
   /// <inheritdoc/>
   public partial class Moonglade1 : DbContext
   {
      #region DbSets

      /// <summary>
      /// Repository for global::Moonglade.Data.Entities.CalendarEntity - My calendar of public
      /// dates
      /// </summary>
      public virtual Microsoft.EntityFrameworkCore.DbSet<global::Moonglade.Data.Entities.CalendarEntity> CalendarEntity { get; set; }

      /// <summary>
      /// Repository for global::Moonglade.Data.CertificateEntity - Table for my certificates
      /// </summary>
      public virtual Microsoft.EntityFrameworkCore.DbSet<global::Moonglade.Data.CertificateEntity> CertificateEntity { get; set; }

      /// <summary>
      /// Repository for global::Moonglade.Data.HonoraryPositonEntity - Table for our Honorary
      /// Positions
      /// </summary>
      public virtual Microsoft.EntityFrameworkCore.DbSet<global::Moonglade.Data.HonoraryPositonEntity> HonoraryPositonEntity { get; set; }
      public virtual Microsoft.EntityFrameworkCore.DbSet<global::Moonglade.Data.Entities.LanguageEnum> LanguageEnum { get; set; }

      /// <summary>
      /// Repository for global::Moonglade.Data.MandateEntity - My Mandates
      /// </summary>
      public virtual Microsoft.EntityFrameworkCore.DbSet<global::Moonglade.Data.MandateEntity> MandateEntity { get; set; }

      /// <summary>
      /// Repository for global::Moonglade.Data.MembershipEntity - Table for Memberships
      /// </summary>
      public virtual Microsoft.EntityFrameworkCore.DbSet<global::Moonglade.Data.MembershipEntity> MembershipEntity { get; set; }

      /// <summary>
      /// Repository for global::Moonglade.Data.ProjectEntity - Table with my Projects
      /// </summary>
      public virtual Microsoft.EntityFrameworkCore.DbSet<global::Moonglade.Data.ProjectEntity> ProjectEntity { get; set; }

      /// <summary>
      /// Repository for global::Moonglade.Data.PublicationEntity - My Publications
      /// </summary>
      public virtual Microsoft.EntityFrameworkCore.DbSet<global::Moonglade.Data.PublicationEntity> PublicationEntity { get; set; }

      /// <summary>
      /// Repository for global::Moonglade.Data.TalkEntity - Table of my held talks
      /// </summary>
      public virtual Microsoft.EntityFrameworkCore.DbSet<global::Moonglade.Data.TalkEntity> TalkEntity { get; set; }
      public virtual Microsoft.EntityFrameworkCore.DbSet<global::Moonglade.Data.Entities.TalkType> TalkType { get; set; }

      /// <summary>
      /// Repository for global::Moonglade.Data.TestimonialEntity - My Testimonials
      /// </summary>
      public virtual Microsoft.EntityFrameworkCore.DbSet<global::Moonglade.Data.TestimonialEntity> TestimonialEntity { get; set; }

      /// <summary>
      /// Repository for global::Moonglade.Data.VideoEntity - Table of my Videos
      /// </summary>
      public virtual Microsoft.EntityFrameworkCore.DbSet<global::Moonglade.Data.VideoEntity> VideoEntity { get; set; }
      public virtual Microsoft.EntityFrameworkCore.DbSet<global::Moonglade.Data.Entities.VideoType> VideoType { get; set; }

      #endregion DbSets

      /// <summary>
      /// Default connection string
      /// </summary>
      public static string ConnectionString { get; set; } = @"https://test.de";

      /// <summary>
      ///     <para>
      ///         Initializes a new instance of the <see cref="T:Microsoft.EntityFrameworkCore.DbContext" /> class using the specified options.
      ///         The <see cref="M:Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)" /> method will still be called to allow further
      ///         configuration of the options.
      ///     </para>
      /// </summary>
      /// <param name="options">The options for this context.</param>
      public Moonglade1(DbContextOptions<Moonglade1> options) : base(options)
      {
      }

      partial void CustomInit(DbContextOptionsBuilder optionsBuilder);

      /// <inheritdoc />
      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      {
         optionsBuilder.UseLazyLoadingProxies();

         CustomInit(optionsBuilder);
      }

      partial void OnModelCreatingImpl(ModelBuilder modelBuilder);
      partial void OnModelCreatedImpl(ModelBuilder modelBuilder);

      /// <summary>
      ///     Override this method to further configure the model that was discovered by convention from the entity types
      ///     exposed in <see cref="T:Microsoft.EntityFrameworkCore.DbSet`1" /> properties on your derived context. The resulting model may be cached
      ///     and re-used for subsequent instances of your derived context.
      /// </summary>
      /// <remarks>
      ///     If a model is explicitly set on the options for this context (via <see cref="M:Microsoft.EntityFrameworkCore.DbContextOptionsBuilder.UseModel(Microsoft.EntityFrameworkCore.Metadata.IModel)" />)
      ///     then this method will not be run.
      /// </remarks>
      /// <param name="modelBuilder">
      ///     The builder being used to construct the model for this context. Databases (and other extensions) typically
      ///     define extension methods on this object that allow you to configure aspects of the model that are specific
      ///     to a given database.
      /// </param>
      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
         base.OnModelCreating(modelBuilder);
         OnModelCreatingImpl(modelBuilder);

         modelBuilder.HasDefaultSchema("dbo");

         modelBuilder.Entity<global::Moonglade.Data.Entities.CalendarEntity>()
                     .ToTable("Calendar")
                     .HasKey(t => t.Id);
         modelBuilder.Entity<global::Moonglade.Data.Entities.CalendarEntity>()
                     .Property(t => t.Id)
                     .ValueGeneratedOnAdd()
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.Entities.CalendarEntity>()
                     .Property(t => t.EventName)
                     .HasMaxLength(150);
         modelBuilder.Entity<global::Moonglade.Data.Entities.CalendarEntity>()
                     .Property(t => t.Logo)
                     .HasMaxLength(150);
         modelBuilder.Entity<global::Moonglade.Data.Entities.CalendarEntity>()
                     .Property(t => t.Link)
                     .HasMaxLength(200);
         modelBuilder.Entity<global::Moonglade.Data.Entities.CalendarEntity>()
                     .Property(t => t.Location)
                     .HasMaxLength(50);

         modelBuilder.Entity<global::Moonglade.Data.CertificateEntity>()
                     .ToTable("Certificates")
                     .HasKey(t => t.Id);
         modelBuilder.Entity<global::Moonglade.Data.CertificateEntity>()
                     .Property(t => t.Id)
                     .ValueGeneratedNever()
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.CertificateEntity>()
                     .Property(t => t.Provider)
                     .HasMaxLength(150)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.CertificateEntity>()
                     .Property(t => t.CertificateTite)
                     .HasMaxLength(150)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.CertificateEntity>()
                     .Property(t => t.Year)
                     .HasMaxLength(50)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.CertificateEntity>()
                     .Property(t => t.Content)
                     .HasMaxLength(300)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.CertificateEntity>()
                     .Property(t => t.Link)
                     .HasMaxLength(300)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.CertificateEntity>()
                     .Property(t => t.Image)
                     .HasMaxLength(300)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.CertificateEntity>()
                     .Property(t => t.Landscape)
                     .IsRequired();

         modelBuilder.Entity<global::Moonglade.Data.HonoraryPositonEntity>()
                     .ToTable("HonoraryPositons")
                     .HasKey(t => t.Id);
         modelBuilder.Entity<global::Moonglade.Data.HonoraryPositonEntity>()
                     .Property(t => t.Id)
                     .ValueGeneratedNever()
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.HonoraryPositonEntity>()
                     .Property(t => t.Link)
                     .HasMaxLength(150)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.HonoraryPositonEntity>()
                     .Property(t => t.Organization)
                     .HasMaxLength(100)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.HonoraryPositonEntity>()
                     .Property(t => t.Summary)
                     .HasMaxLength(200)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.HonoraryPositonEntity>()
                     .Property(t => t.Active)
                     .IsRequired();

         modelBuilder.Entity<global::Moonglade.Data.Entities.LanguageEnum>()
                     .ToTable("LanguageEnum")
                     .HasNoKey();

         modelBuilder.Entity<global::Moonglade.Data.MandateEntity>()
                     .ToTable("Mandates")
                     .HasKey(t => t.Id);
         modelBuilder.Entity<global::Moonglade.Data.MandateEntity>()
                     .Property(t => t.Id)
                     .ValueGeneratedNever()
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.MandateEntity>()
                     .Property(t => t.Link)
                     .HasMaxLength(150)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.MandateEntity>()
                     .Property(t => t.Organization)
                     .HasMaxLength(150)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.MandateEntity>()
                     .Property(t => t.Summary)
                     .HasMaxLength(150)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.MandateEntity>()
                     .Property(t => t.Years)
                     .HasMaxLength(50)
                     .IsRequired();

         modelBuilder.Entity<global::Moonglade.Data.MembershipEntity>()
                     .ToTable("Memberships")
                     .HasKey(t => t.Id);
         modelBuilder.Entity<global::Moonglade.Data.MembershipEntity>()
                     .Property(t => t.Id)
                     .ValueGeneratedNever()
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.MembershipEntity>()
                     .Property(t => t.Link)
                     .HasMaxLength(100)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.MembershipEntity>()
                     .Property(t => t.Organzation)
                     .HasMaxLength(150)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.MembershipEntity>()
                     .Property(t => t.Summary)
                     .HasMaxLength(200)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.MembershipEntity>()
                     .Property(t => t.Active)
                     .IsRequired();

         modelBuilder.Entity<global::Moonglade.Data.ProjectEntity>()
                     .ToTable("Projects")
                     .HasKey(t => t.Id);
         modelBuilder.Entity<global::Moonglade.Data.ProjectEntity>()
                     .Property(t => t.Id)
                     .ValueGeneratedNever()
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.ProjectEntity>()
                     .Property(t => t.PortfolioLink)
                     .HasMaxLength(150)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.ProjectEntity>()
                     .Property(t => t.ProjectLink)
                     .HasMaxLength(150)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.ProjectEntity>()
                     .Property(t => t.ProjectName)
                     .HasMaxLength(100)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.ProjectEntity>()
                     .Property(t => t.ProjectSummary)
                     .HasMaxLength(200)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.ProjectEntity>()
                     .Property(t => t.Client)
                     .HasMaxLength(50)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.ProjectEntity>()
                     .Property(t => t.Completion)
                     .HasColumnName("datetime")
                     .HasColumnType("datetime")
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.ProjectEntity>()
                     .Property(t => t.ProjectType)
                     .HasMaxLength(50)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.ProjectEntity>()
                     .Property(t => t.Authors)
                     .HasMaxLength(50)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.ProjectEntity>()
                     .Property(t => t.Language)
                     .IsRequired();

         modelBuilder.Entity<global::Moonglade.Data.PublicationEntity>()
                     .ToTable("Publications")
                     .HasKey(t => t.Id);
         modelBuilder.Entity<global::Moonglade.Data.PublicationEntity>()
                     .Property(t => t.Id)
                     .ValueGeneratedNever()
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.PublicationEntity>()
                     .Property(t => t.PublicationName)
                     .HasMaxLength(100)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.PublicationEntity>()
                     .Property(t => t.Publisher)
                     .HasMaxLength(50)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.PublicationEntity>()
                     .Property(t => t.DatePublished)
                     .HasColumnType("datetime")
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.PublicationEntity>()
                     .Property(t => t.Authors)
                     .HasMaxLength(100)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.PublicationEntity>()
                     .Property(t => t.IsBook)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.PublicationEntity>()
                     .Property(t => t.Title)
                     .HasMaxLength(100)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.PublicationEntity>()
                     .Property(t => t.Link)
                     .HasMaxLength(150)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.PublicationEntity>()
                     .Property(t => t.Identifier)
                     .HasMaxLength(50)
                     .IsRequired();

         modelBuilder.Entity<global::Moonglade.Data.TalkEntity>()
                     .ToTable("Talks")
                     .HasKey(t => t.Id);
         modelBuilder.Entity<global::Moonglade.Data.TalkEntity>()
                     .Property(t => t.Id)
                     .ValueGeneratedNever()
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.TalkEntity>()
                     .Property(t => t.Title)
                     .HasMaxLength(150)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.TalkEntity>()
                     .Property(t => t.Where)
                     .HasMaxLength(50)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.TalkEntity>()
                     .Property(t => t.Link)
                     .HasMaxLength(150)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.TalkEntity>()
                     .Property(t => t.Summary)
                     .HasMaxLength(150)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.TalkEntity>()
                     .Property(t => t.Date)
                     .HasColumnType("datetime")
                     .IsRequired();

         modelBuilder.Entity<global::Moonglade.Data.Entities.TalkType>()
                     .ToTable("TalkType")
                     .HasNoKey();

         modelBuilder.Entity<global::Moonglade.Data.TestimonialEntity>()
                     .ToTable("Testimonials")
                     .HasKey(t => t.Id);
         modelBuilder.Entity<global::Moonglade.Data.TestimonialEntity>()
                     .Property(t => t.Id)
                     .ValueGeneratedNever()
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.TestimonialEntity>()
                     .Property(t => t.Link)
                     .HasMaxLength(150)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.TestimonialEntity>()
                     .Property(t => t.Summary)
                     .HasMaxLength(600)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.TestimonialEntity>()
                     .Property(t => t.Recommender)
                     .HasMaxLength(50)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.TestimonialEntity>()
                     .Property(t => t.RecommenderJob)
                     .HasMaxLength(50)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.TestimonialEntity>()
                     .Property(t => t.RecommenderLocation)
                     .HasMaxLength(50)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.TestimonialEntity>()
                     .Property(t => t.Relationship)
                     .HasMaxLength(50)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.TestimonialEntity>()
                     .Property(t => t.ImagePath)
                     .HasMaxLength(150)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.TestimonialEntity>()
                     .Property(t => t.Date)
                     .HasColumnType("datetime")
                     .IsRequired();

         modelBuilder.Entity<global::Moonglade.Data.VideoEntity>()
                     .ToTable("Videos")
                     .HasKey(t => t.Id);
         modelBuilder.Entity<global::Moonglade.Data.VideoEntity>()
                     .Property(t => t.Id)
                     .ValueGeneratedNever()
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.VideoEntity>()
                     .Property(t => t.Title)
                     .HasMaxLength(150)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.VideoEntity>()
                     .Property(t => t.Description)
                     .HasMaxLength(150)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.VideoEntity>()
                     .Property(t => t.VideoCode)
                     .HasMaxLength(20)
                     .IsRequired();
         modelBuilder.Entity<global::Moonglade.Data.VideoEntity>()
                     .Property(t => t.DatePublished)
                     .HasColumnType("datetime");

         modelBuilder.Entity<global::Moonglade.Data.Entities.VideoType>()
                     .ToTable("VideoType")
                     .HasNoKey();

         OnModelCreatedImpl(modelBuilder);
      }
   }
}
