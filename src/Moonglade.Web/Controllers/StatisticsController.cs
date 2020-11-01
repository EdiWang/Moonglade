using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Core;
using Moonglade.Model;
using Moonglade.Web.Filters;

namespace Moonglade.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatisticsController : ControllerBase
    {
        private readonly IBlogStatistics _statistics;
        private bool DNT => (bool)HttpContext.Items["DNT"];

        public StatisticsController(IBlogStatistics statistics)
        {
            _statistics = statistics;
        }

        [HttpGet("{postId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Get(Guid postId)
        {
            if (postId == Guid.Empty) return BadRequest($"{nameof(postId)} is empty");

            var (Hits, Likes) = await _statistics.GetStatisticAsync(postId);
            return Ok(new { Hits, Likes });
        }

        [HttpPost("{postId}")]
        [DisallowSpiderUA]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post(Guid postId, [FromForm] bool isLike = false)
        {
            if (postId == Guid.Empty) return BadRequest("postId is empty");
            if (DNT) return Ok();

            if (isLike)
            {
                if (HasCookie(CookieNames.Liked, postId.ToString())) return Conflict();
            }
            else
            {
                if (HasCookie(CookieNames.Hit, postId.ToString())) return Ok();
            }

            await _statistics.UpdateStatisticAsync(postId, isLike ? 1 : 0);
            SetPostTrackingCookie(isLike ? CookieNames.Liked : CookieNames.Hit, postId.ToString());

            return Ok();
        }

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
    }
}
