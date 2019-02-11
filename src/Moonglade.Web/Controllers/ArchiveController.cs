using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Model.Settings;

namespace Moonglade.Web.Controllers
{
    public class ArchiveController : MoongladeController
    {
        private readonly CategoryService _categoryService;
        private readonly PostService _postService;

        public ArchiveController(MoongladeDbContext context,
            ILogger<PostController> logger,
            IOptions<AppSettings> settings,
            IConfiguration configuration,
            IHttpContextAccessor accessor, CategoryService categoryService, PostService postService)
            : base(context, logger, settings, configuration, accessor)
        {
            _categoryService = categoryService;
            _postService = postService;
        }

        [Route("archive")]
        public IActionResult Index()
        {
            var response = _categoryService.GetArchiveList();
            if (response.IsSuccess)
            {
                return View(response.Item);
            }

            return ServerError();
        }
        
        [Route("archive/{year:int:length(4)}")]
        [Route("archive/{year:int:length(4)}/{month:int:range(1,12)}")]
        public IActionResult GetArchiveByTime(int year, int? month)
        {
            List<Post> postList;

            if (year > DateTime.UtcNow.Year)
            {
                return NotFound();
            }

            if (null != month)
            {
                // {year}/{month}
                ViewBag.CurrentListInfo = $"All Posts of {year}.{month}";
                ViewBag.TitlePrefix = $"All Posts of {year}.{month}";
                postList = _postService.GetArchivedPosts(year, month.Value).ToList();
            }
            else
            {
                // {year}
                ViewBag.CurrentListInfo = $"All Posts of {year}";
                ViewBag.TitlePrefix = $"All Posts of {year}";
                postList = _postService.GetArchivedPosts(year).ToList();
            }

            var model = postList.OrderByDescending(p => p.PostPublish.PubDateUtc).ToList();
            return View(model);
        }
    }
}