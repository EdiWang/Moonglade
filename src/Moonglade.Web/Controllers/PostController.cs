using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Edi.Blog.Pingback;
using Edi.Blog.Pingback.MvcExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Data.Entities;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Web.Models;
using X.PagedList;

namespace Moonglade.Web.Controllers
{
    [Route("post")]
    public partial class PostController : MoongladeController
    {
        private readonly PostService _postService;
        private readonly CategoryService _categoryService;
        private readonly IPingbackSender _pingbackSender;
        private readonly LinkGenerator _linkGenerator;

        public PostController(
            ILogger<PostController> logger,
            IOptions<AppSettings> settings,
            PostService postService,
            CategoryService categoryService,
            IPingbackSender pingbackSender,
            LinkGenerator linkGenerator)
            : base(logger, settings)
        {
            _postService = postService;
            _categoryService = categoryService;
            _pingbackSender = pingbackSender;
            _linkGenerator = linkGenerator;
        }

        [Route(""), Route("/")]
        public async Task<IActionResult> Index(int page = 1)
        {
            int pagesize = AppSettings.PostListPageSize;
            var postList = await _postService.GetPagedPostsAsync(pagesize, page);
            var postsAsIPagedList = new StaticPagedList<Post>(postList, page, pagesize, _postService.CountForPublic);
            return View(postsAsIPagedList);
        }

        [Route("{year:int:min(2008):max(2108):length(4)}/{month:int:range(1,12)}/{day:int:range(1,31)}/{slug}")]
        [AddPingbackHeader("pingback")]
        public async Task<IActionResult> Slug(int year, int month, int day, string slug)
        {
            ViewBag.ErrorMessage = string.Empty;

            if (year > DateTime.UtcNow.Year || string.IsNullOrWhiteSpace(slug))
            {
                Logger.LogWarning($"Invalid parameter year: {year}, slug: {slug}");
                return NotFound();
            }

            var rsp = await _postService.GetPostAsync(year, month, day, slug);
            if (!rsp.IsSuccess) return ServerError(rsp.Message);

            var post = rsp.Item;
            if (post == null)
            {
                Logger.LogWarning($"Post not found, parameter {year}/{month}/{day}/{slug}.");
                return NotFound();
            }

            var viewModel = new PostSlugViewModelWrapper();

            #region Fetch Post Main Model

            var postModel = new PostSlugViewModel
            {
                Title = post.Title,
                Abstract = post.ContentAbstract,
                PubDateUtc = post.PostPublish.PubDateUtc.GetValueOrDefault(),

                Categories = post.PostCategory.Select(pc => pc.Category).Select(p => new SimpleCategoryInfoViewModel
                {
                    CategoryDisplayName = p.DisplayName,
                    CategoryRouteName = p.Title
                }).ToList(),

                Content = HttpUtility.HtmlDecode(post.PostContent),
                Hits = post.PostExtension.Hits,
                Likes = post.PostExtension.Likes,

                Tags = post.PostTag.Select(pt => pt.Tag)
                                   .Select(p => new TagInfo
                                   {
                                       NormalizedTagName = p.NormalizedName,
                                       TagName = p.DisplayName
                                   }).ToList(),
                PostId = post.Id.ToString(),
                CommentEnabled = post.CommentEnabled,
                IsExposedToSiteMap = post.PostPublish.ExposedToSiteMap,
                LastModifyOnUtc = post.PostPublish.LastModifiedUtc,
                CommentCount = post.Comment.Count(c => c.IsApproved)
            };

            if (AppSettings.EnableImageLazyLoad)
            {
                var rawHtmlContent = postModel.Content;
                postModel.Content = Utils.ReplaceImgSrc(rawHtmlContent);
            }

            viewModel.PostModel = postModel;
            viewModel.NewCommentModel.PostId = post.Id;

            #endregion Fetch Post Main Model

            ViewBag.TitlePrefix = $"{post.Title}";
            return View(viewModel);
        }

        [HttpPost("hit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hit([FromForm] Guid postId)
        {
            if (HasCookie(CookieNames.Hit, postId.ToString()))
            {
                return new EmptyResult();
            }

            var response = await _postService.UpdatePostStatisticAsync(postId, StatisticType.Hits);
            if (response.IsSuccess)
            {
                SetPostTrackingCookie(CookieNames.Hit, postId.ToString());
            }

            return Json(response);
        }

        [HttpPost("like")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like([FromForm] Guid postId)
        {
            if (HasCookie(CookieNames.Liked, postId.ToString()))
            {
                return Json(new
                {
                    IsSuccess = false,
                    Message = "You Have Rated"
                });
            }

            var response = await _postService.UpdatePostStatisticAsync(postId, StatisticType.Likes);
            if (response.IsSuccess)
            {
                SetPostTrackingCookie(CookieNames.Liked, postId.ToString());
            }

            return Json(response);
        }

        #region Helper Methods

        private bool HasCookie(CookieNames cookieName, string id)
        {
            var viewCookie = HttpContext.Request.Cookies[cookieName.ToString()];
            if (viewCookie != null)
            {
                return viewCookie == id;
            }
            return false;
        }

        private void SetPostTrackingCookie(CookieNames cookieName, string id)
        {
            var options = new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(1),
                SameSite = SameSiteMode.Strict,
                Secure = Request.IsHttps,

                // Mark as essential to pass GDPR
                // https://docs.microsoft.com/en-us/aspnet/core/security/gdpr?view=aspnetcore-2.1
                IsEssential = true
            };

            Response.Cookies.Append(cookieName.ToString(), id, options);
        }

        #endregion
    }
}
