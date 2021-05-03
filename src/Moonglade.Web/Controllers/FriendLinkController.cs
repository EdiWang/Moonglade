using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.FriendLink;
using Moonglade.Utils;
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
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(FriendLinkEditModel viewModel)
        {
            await _friendLinkService.AddAsync(viewModel.Title, viewModel.LinkUrl);
            return Created(string.Empty, viewModel);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Link), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(Guid id)
        {
            try
            {
                var link = await _friendLinkService.GetAsync(id);
                if (null == link) return NotFound();

                return Ok(link);
            }
            catch (Exception e)
            {
                ModelState.AddModelError(string.Empty, e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Edit(Guid id, FriendLinkEditModel viewModel)
        {
            await _friendLinkService.UpdateAsync(id, viewModel.Title, viewModel.LinkUrl);
            return Ok(viewModel);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty)
            {
                ModelState.AddModelError(nameof(id), "value is empty");
                return BadRequest(ModelState.CombineErrorMessages());
            }

            await _friendLinkService.DeleteAsync(id);
            return Ok();
        }
    }
}
