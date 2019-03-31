using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.Core;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    [Route("archive")]
    public class ArchiveController : MoongladeController
    {
        private readonly CategoryService _categoryService;
        private readonly PostService _postService;

        public ArchiveController(MoongladeDbContext context,
            ILogger<PostController> logger,
            CategoryService categoryService, 
            PostService postService)
            : base(context, logger)
        {
            _categoryService = categoryService;
            _postService = postService;
        }
        
        [Route("")]
        public async Task<IActionResult> Index()
        {
            var response = await _categoryService.GetArchiveListAsync();
            return response.IsSuccess ? View(response.Item) : ServerError();
        }

        [Route("{year:int:length(4)}")]
        [Route("{year:int:length(4)}/{month:int:range(1,12)}")]
        public async Task<IActionResult> GetArchive(int year, int? month)
        {
            if (year > DateTime.UtcNow.Year)
            {
                return NotFound();
            }

            IQueryable<Post> postListQuery;
            if (null != month)
            {
                // {year}/{month}
                ViewBag.ArchiveInfo = $"{year}.{month}";
                postListQuery = _postService.GetArchivedPosts(year, month.Value);
            }
            else
            {
                // {year}
                ViewBag.ArchiveInfo = $"{year}";
                postListQuery = _postService.GetArchivedPosts(year);
            }

            var model = await postListQuery.OrderByDescending(p => p.PostPublish.PubDateUtc)
                                           .Select(p => new PostArchiveItemViewModel
                                           {
                                               PubDateUtc = p.PostPublish.PubDateUtc.GetValueOrDefault(),
                                               Slug = p.Slug,
                                               Title = p.Title
                                           })
                                           .ToListAsync();

            return View(model);
        }
    }
}