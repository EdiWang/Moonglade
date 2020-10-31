using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Core;
using Moonglade.Model;
using Moonglade.Web.Filters;

namespace Moonglade.Web.Controllers
{
    [Route("api/statistics")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly PostService _postService;
        private bool DNT => (bool)HttpContext.Items["DNT"];

        public StatisticsController(PostService postService)
        {
            _postService = postService;
        }

        [HttpPost("hit")]
        [DisallowSpiderUA]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Hit([FromForm] Guid postId)
        {
            if (postId == Guid.Empty) return BadRequest("postId is empty");
            if (DNT || HasCookie(CookieNames.Hit, postId.ToString())) return Ok();

            await _postService.UpdateStatisticAsync(postId);
            SetPostTrackingCookie(CookieNames.Hit, postId.ToString());

            return Ok();
        }

        [HttpPost("like")]
        [DisallowSpiderUA]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Like([FromForm] Guid postId)
        {
            if (postId == Guid.Empty) return BadRequest("postId is empty");
            if (DNT) return Ok();
            if (HasCookie(CookieNames.Liked, postId.ToString())) return Conflict();

            await _postService.UpdateStatisticAsync(postId, 1);
            SetPostTrackingCookie(CookieNames.Liked, postId.ToString());

            return Ok();
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
