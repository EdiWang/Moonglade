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

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(FriendLinkEditModel viewModel)
        {
            await _friendLinkService.AddAsync(viewModel.Title, viewModel.LinkUrl);
            return Ok(viewModel);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(Guid id)
        {
            try
            {
                var link = await _friendLinkService.GetAsync(id);
                if (null == link) return NotFound();

                var obj = new FriendLinkEditModel
                {
                    Id = link.Id,
                    LinkUrl = link.LinkUrl,
                    Title = link.Title
                };

                return Ok(obj);
            }
            catch (Exception e)
            {
                ModelState.AddModelError(string.Empty, e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPut("edit")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Edit(FriendLinkEditModel viewModel)
        {
            await _friendLinkService.UpdateAsync(viewModel.Id, viewModel.Title, viewModel.LinkUrl);
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
