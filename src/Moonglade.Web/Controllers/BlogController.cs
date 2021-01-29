using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Moonglade.Web.Controllers
{
    public class BlogController : Controller
    {
        protected string SiteIconDirectory => Path.Join(AppDomain.CurrentDomain.GetData("DataDirectory")?.ToString(), "siteicons");

        [Route("server-error")]
        public IActionResult ServerError(string errMessage = null)
        {
            if (string.IsNullOrWhiteSpace(errMessage))
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return StatusCode(StatusCodes.Status500InternalServerError, errMessage);
        }
    }
}