using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Model;

namespace Moonglade.Web.Controllers
{
    public class BlogController : Controller
    {
        protected string DataDirectory => AppDomain.CurrentDomain.GetData(Constants.DataDirectory)?.ToString();

        protected string SiteIconDirectory => Path.Join(DataDirectory, "siteicons");

        protected bool DNT => (bool)HttpContext.Items["DNT"];

        [Route("server-error")]
        public IActionResult ServerError(string errMessage = null)
        {
            if (string.IsNullOrWhiteSpace(errMessage))
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError, errMessage);
            }
        }

        [NonAction]
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

        [NonAction]
        protected string ResolveRootUrl(IBlogConfig blogConfig, bool preferCanonical = false)
        {
            var requestedRoot = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
            if (!preferCanonical) return requestedRoot;

            var url = Utils.ResolveCanonicalUrl(blogConfig.GeneralSettings.CanonicalPrefix, string.Empty);
            if (string.IsNullOrWhiteSpace(url)) return requestedRoot;
            return url;
        }
    }
}