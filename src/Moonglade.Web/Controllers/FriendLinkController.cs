using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.FriendLink;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FriendLinkController : ControllerBase
    {
        private readonly IFriendLinkService _friendLinkService;

        public FriendLinkController(IFriendLinkService friendLinkService)
        {
            _friendLinkService = friendLinkService;
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
            var link = await _friendLinkService.GetAsync(id);
            if (null == link) return NotFound();

            return Ok(link);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(FriendLinkEditModel),StatusCodes.Status200OK)]
        public async Task<IActionResult> Edit([NotEmpty] Guid id, FriendLinkEditModel model)
        {
            await _friendLinkService.UpdateAsync(id, model.Title, model.LinkUrl);
            return Ok(model);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([NotEmpty] Guid id)
        {
            await _friendLinkService.DeleteAsync(id);
            return Ok();
        }
    }
}
