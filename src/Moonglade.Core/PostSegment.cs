using Moonglade.Data.Entities;
using System;
using System.Linq.Expressions;

namespace Moonglade.Core
{
    public struct PostSegment
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string ContentAbstract { get; set; }
        public DateTime? PubDateUtc { get; set; }
        public DateTime CreateTimeUtc { get; set; }
        public DateTime? LastModifiedUtc { get; set; }
        public bool IsPublished { get; set; }
        public int Hits { get; set; }
        public bool IsDeleted { get; set; }

        public static readonly Expression<Func<PostEntity, PostSegment>> EntitySelector = p => new()
        {
            Id = p.Id,
            Title = p.Title,
            Slug = p.Slug,
            PubDateUtc = p.PubDateUtc,
            IsPublished = p.IsPublished,
            IsDeleted = p.IsDeleted,
            CreateTimeUtc = p.CreateTimeUtc,
            LastModifiedUtc = p.LastModifiedUtc,
            ContentAbstract = p.ContentAbstract,
            Hits = p.PostExtension.Hits
        };
    }
}
