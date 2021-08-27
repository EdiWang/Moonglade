using Moonglade.Data.Entities;
using System;

namespace Moonglade.Core
{
    public class BlogPage
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string MetaDescription { get; set; }
        public string RawHtmlContent { get; set; }
        public string CssContent { get; set; }
        public bool HideSidebar { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreateTimeUtc { get; set; }
        public DateTime? UpdateTimeUtc { get; set; }

        public BlogPage()
        {

        }

        public BlogPage(PageEntity entity)
        {
            if (entity is null) return;

            Id = entity.Id;
            Title = entity.Title.Trim();
            CreateTimeUtc = entity.CreateTimeUtc;
            CssContent = entity.CssContent;
            RawHtmlContent = entity.HtmlContent;
            HideSidebar = entity.HideSidebar;
            Slug = entity.Slug.Trim().ToLower();
            MetaDescription = entity.MetaDescription?.Trim();
            UpdateTimeUtc = entity.UpdateTimeUtc;
            IsPublished = entity.IsPublished;
        }
    }
}
