using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Core;
using Moonglade.Model;

namespace Moonglade.Web.Controllers
{
    [Route("archive")]
    public class ArchiveController : MoongladeController
    {
        private readonly PostService _postService;

        public ArchiveController(
            ILogger<PostController> logger,
            PostService postService)
            : base(logger)
        {
            _postService = postService;
        }

        [Route("")]
        public async Task<IActionResult> Index()
        {
            var response = await _postService.GetArchiveListAsync();
            if (!response.IsSuccess)
            {
                SetFriendlyErrorMessage();
            }
            return View(response.Item);
        }

        [HttpGet("{year:int:length(4)}")]
        [HttpGet("{year:int:length(4)}/{month:int:range(1,12)}")]
        public async Task<IActionResult> List(int year, int? month)
        {
            if (year > DateTime.UtcNow.Year)
            {
                return NotFound();
            }

            IReadOnlyList<PostListItem> model;

            if (null != month)
            {
                // {year}/{month}
                ViewBag.ArchiveInfo = $"{year}.{month}";
                model = await _postService.GetArchivedPostsAsync(year, month.Value);
            }
            else
            {
                // {year}
                ViewBag.ArchiveInfo = $"{year}";
                model = await _postService.GetArchivedPostsAsync(year);
            }

            return View(model);
        }
    }
}