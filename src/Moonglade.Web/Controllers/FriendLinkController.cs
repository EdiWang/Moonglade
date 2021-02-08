using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.FriendLink;
using Moonglade.Web.Filters;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [ApiController]
    [AppendAppVersion]
    [Route("api/[controller]")]
    public class FriendLinkController : ControllerBase
    {
        private readonly IFriendLinkService _friendLinkService;

        public FriendLinkController(IFriendLinkService friendLinkService)
        {
            _friendLinkService = friendLinkService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(FriendLinkEditViewModel viewModel)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _friendLinkService.AddAsync(viewModel.Title, viewModel.LinkUrl);
            return Ok(viewModel);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            try
            {
                var link = await _friendLinkService.GetAsync(id);
                if (null == link) return NotFound();

                var obj = new FriendLinkEditViewModel
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

        [HttpPost("edit")]
        public async Task<IActionResult> Edit(FriendLinkEditViewModel viewModel)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _friendLinkService.UpdateAsync(viewModel.Id, viewModel.Title, viewModel.LinkUrl);
            return Ok(viewModel);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty)
            {
                ModelState.AddModelError(nameof(id), "value is empty");
                return BadRequest(ModelState);
            }

            await _friendLinkService.DeleteAsync(id);
            return Ok();
        }
    }
}
