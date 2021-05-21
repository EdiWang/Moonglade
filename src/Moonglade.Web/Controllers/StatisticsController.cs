using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Core;
using Moonglade.Utils;
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
                return BadRequest(ModelState.CombineErrorMessages());
            }

            var (hits, likes) = await _statistics.GetStatisticAsync(postId);
            return Ok(new { Hits = hits, Likes = likes });
        }

        [HttpPost]
        [DisallowSpiderUA]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post(StatisticsRequest request)
        {
            if (request.PostId == Guid.Empty)
            {
                ModelState.AddModelError(nameof(request.PostId), "value is empty");
                return BadRequest(ModelState.CombineErrorMessages());
            }

            if (DNT) return Ok();

            await _statistics.UpdateStatisticAsync(request.PostId, request.IsLike);
            return Ok();
        }
    }

    public class StatisticsRequest
    {
        public Guid PostId { get; set; }

        public bool IsLike { get; set; }
    }
}
