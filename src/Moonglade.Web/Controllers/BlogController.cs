using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moonglade.Model;

namespace Moonglade.Web.Controllers
{
    public class BlogController : Controller
    {
        protected ILogger<ControllerBase> Logger { get; }

        protected string RootUrl => $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

        protected string DataDirectory => AppDomain.CurrentDomain.GetData(Constants.DataDirectory)?.ToString();

        protected string SiteIconDirectory => Path.Join(DataDirectory, "siteicons");

        protected bool DNT => (bool)HttpContext.Items["DNT"];

        public BlogController(ILogger<ControllerBase> logger)
        {
            if (null != logger) Logger = logger;
        }

        [Route("server-error")]
        public IActionResult ServerError(string errMessage = "")
        {
            if (!string.IsNullOrWhiteSpace(errMessage))
            {
                Logger.LogError($"Server Error: {errMessage}");
            }
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        protected void SetFriendlyErrorMessage()
        {
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            ViewBag.IsServerError = true;
        }

        protected string GetPostUrl(LinkGenerator linkGenerator, DateTime pubDate, string slug)
        {
            var link = linkGenerator.GetUriByAction(HttpContext, "Slug", "Post",
                new
                {
                    year = pubDate.Year,
                    month = pubDate.Month,
                    day = pubDate.Day,
                    slug
                });
            return link;
        }
    }
}