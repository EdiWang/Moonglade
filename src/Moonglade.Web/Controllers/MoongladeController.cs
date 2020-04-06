using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Web.Controllers
{
    public class MoongladeController : Controller
    {
        protected ILogger<ControllerBase> Logger { get; }

        protected AppSettings AppSettings { get; set; }

        protected string SiteRootUrl => $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

        protected string SiteRootDirectory => AppDomain.CurrentDomain.GetData(Constants.AppBaseDirectory).ToString();

        protected string SiteDataDirectory => AppDomain.CurrentDomain.GetData(Constants.DataDirectory).ToString();

        protected string SiteIconDirectory => Path.Join(SiteDataDirectory, "siteicons");

        public MoongladeController(
            ILogger<ControllerBase> logger,
            IOptions<AppSettings> settings = null)
        {
            if (null != logger) Logger = logger;
            if (null != settings) AppSettings = settings.Value;
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

        protected string GetUserAgent()
        {
            return Request.Headers["User-Agent"];
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