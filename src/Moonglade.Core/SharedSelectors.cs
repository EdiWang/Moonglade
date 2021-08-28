using Moonglade.Data.Entities;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Moonglade.Core
{
    internal class SharedSelectors
    {
        public static Expression<Func<PostEntity, PostDigest>> PostDigestSelector => p => new()
        {
            Title = p.Title,
            Slug = p.Slug,
            ContentAbstract = p.ContentAbstract,
            PubDateUtc = p.PubDateUtc.GetValueOrDefault(),
            LangCode = p.ContentLanguageCode,
            IsFeatured = p.IsFeatured,
            Tags = p.Tags.Select(pt => new Tag
            {
                NormalizedName = pt.NormalizedName,
                DisplayName = pt.DisplayName
            })
        };

        public static readonly Expression<Func<TagEntity, Tag>> TagSelector = t => new()
        {
            Id = t.Id,
            NormalizedName = t.NormalizedName,
            DisplayName = t.DisplayName
        };
    }
}
