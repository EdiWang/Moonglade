using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Web.Models;

namespace Moonglade.Web.Pages.Admin
{
    public class EditPostModel : PageModel
    {
        private readonly ICategoryService _catService;
        private readonly IPostService _postService;
        private readonly ITimeZoneResolver _timeZoneResolver;

        public PostEditModel ViewModel { get; set; }

        public EditPostModel(
            ICategoryService catService,
            IPostService postService,
            ITimeZoneResolver timeZoneResolver,
            IBlogConfig blogConfig)
        {
            _catService = catService;
            _postService = postService;
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
                        new CheckBoxViewModel(p.DisplayName, p.Id.ToString(), false));
                    ViewModel.CategoryList = cbCatList;
                }

                return Page();
            }

            var post = await _postService.GetAsync(id.Value);
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
                Featured = post.Featured
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
                    new CheckBoxViewModel(
                        p.DisplayName,
                        p.Id.ToString(),
                        post.Categories.Any(q => q.Id == p.Id)));
                ViewModel.CategoryList = cbCatList;
            }

            return Page();
        }
    }
}
