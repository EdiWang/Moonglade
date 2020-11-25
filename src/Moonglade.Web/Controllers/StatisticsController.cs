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

        [HttpGet("{postId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Get(Guid postId)
        {
            if (postId == Guid.Empty)
            {
                ModelState.AddModelError(nameof(postId), "value is empty");
                return BadRequest(ModelState);
            }

            var (Hits, Likes) = await _statistics.GetStatisticAsync(postId);
            return Ok(new { Hits, Likes });
        }

        [HttpPost]
        [DisallowSpiderUA]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post(StatisticsRequest request)
        {
            if (request.PostId == Guid.Empty)
            {
                ModelState.AddModelError(nameof(request.PostId), "value is empty");
                return BadRequest(ModelState);
            }

            if (DNT) return Ok();

            if (request.IsLike)
            {
                if (HasCookie(CookieNames.Liked, request.PostId.ToString())) return Conflict();
            }
            else
            {
                if (HasCookie(CookieNames.Hit, request.PostId.ToString())) return Ok();
            }

            await _statistics.UpdateStatisticAsync(request.PostId, request.IsLike ? 1 : 0);
            SetPostTrackingCookie(request.IsLike ? CookieNames.Liked : CookieNames.Hit, request.PostId.ToString());

            return Ok();
        }

        private bool HasCookie(CookieNames cookieName, string id)
        {
            var viewCookie = HttpContext.Request.Cookies[cookieName.ToString()];
            if (viewCookie is not null) return viewCookie == id;
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

    public class StatisticsRequest
    {
        public Guid PostId { get; set; }

        public bool IsLike { get; set; }
    }
}
