using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.FriendLink;
using Moonglade.Web.Models;
using System;
using System.Threading.Tasks;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FriendLinkController : ControllerBase
    {
        private readonly IFriendLinkService _friendLinkService;
        private readonly IMediator _mediator;

        public FriendLinkController(IFriendLinkService friendLinkService, IMediator mediator)
        {
            _friendLinkService = friendLinkService;
            _mediator = mediator;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> Create(FriendLinkEditModel model)
        {
            await _friendLinkService.AddAsync(model.Title, model.LinkUrl);
            return Created(string.Empty, model);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Link), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get([NotEmpty] Guid id)
        {
            var link = await _mediator.Send(new GetLinkQuery(id));
            if (null == link) return NotFound();

            return Ok(link);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Update([NotEmpty] Guid id, FriendLinkEditModel model)
        {
            await _friendLinkService.UpdateAsync(id, model.Title, model.LinkUrl);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete([NotEmpty] Guid id)
        {
            await _friendLinkService.DeleteAsync(id);
            return NoContent();
        }
    }
}
