using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.Web.Models;

namespace Moonglade.Web.Pages.Admin
{
    public class EditPostModel : PageModel
    {
        private readonly ICategoryService _catService;
        private readonly IPostQueryService _postQueryService;
        private readonly ITimeZoneResolver _timeZoneResolver;

        public PostEditModel ViewModel { get; set; }

        public EditPostModel(
            ICategoryService catService,
            IPostQueryService postQueryService,
            ITimeZoneResolver timeZoneResolver,
            IBlogConfig blogConfig)
        {
            _catService = catService;
            _postQueryService = postQueryService;
            _timeZoneResolver = timeZoneResolver;
            ViewModel = new()
            {
                IsPublished = false,
                Featured = false,
                EnableComment = true,
                ExposedToSiteMap = true,
                FeedIncluded = true,
                LanguageCode = blogConfig.ContentSettings.DefaultLangCode
            };
        }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id is null)
            {
                var cats1 = await _catService.GetAll();
                if (cats1.Count > 0)
                {
                    var cbCatList = cats1.Select(p =>
                        new CategoryCheckBox
                        {
                            Id = p.Id,
                            DisplayText = p.DisplayName,
                            IsChecked = false
                        });

                    ViewModel.CategoryList = cbCatList.ToList();
                }

                return Page();
            }

            var post = await _postQueryService.GetAsync(id.Value);
            if (null == post) return NotFound();

            ViewModel = new()
            {
                PostId = post.Id,
                IsPublished = post.IsPublished,
                EditorContent = post.RawPostContent,
                Slug = post.Slug,
                Title = post.Title,
                EnableComment = post.CommentEnabled,
                ExposedToSiteMap = post.ExposedToSiteMap,
                FeedIncluded = post.IsFeedIncluded,
                LanguageCode = post.ContentLanguageCode,
                Abstract = post.ContentAbstract.Replace("\u00A0\u2026", string.Empty),
                Featured = post.Featured,
                IsOriginal = post.IsOriginal,
                OriginLink = post.OriginLink,
                HeroImageUrl = post.HeroImageUrl
            };

            if (post.PubDateUtc is not null)
            {
                ViewModel.PublishDate = _timeZoneResolver.ToTimeZone(post.PubDateUtc.GetValueOrDefault());
            }

            var tagStr = post.Tags
                .Select(p => p.DisplayName)
                .Aggregate(string.Empty, (current, item) => current + item + ",");

            tagStr = tagStr.TrimEnd(',');
            ViewModel.Tags = tagStr;

            var cats2 = await _catService.GetAll();
            if (cats2.Count > 0)
            {
                var cbCatList = cats2.Select(p =>
                    new CategoryCheckBox
                    {
                        Id = p.Id,
                        DisplayText = p.DisplayName,
                        IsChecked = post.Categories.Any(q => q.Id == p.Id)
                    });
                ViewModel.CategoryList = cbCatList.ToList();
            }

            return Page();
        }
    }
}
