﻿using LiteBus.Commands.Abstractions;
using Moonglade.Core.PostFeature;
using Moonglade.Web.Attributes;

namespace Moonglade.Web.Controllers;

[Authorize]
[Route("api/post")]
[ApiController]
[TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.Subscription | BlogCacheType.SiteMap])]
public class RecycleBinController(ICommandMediator commandMediator) : ControllerBase
{
    [HttpPost("{postId:guid}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Restore([NotEmpty] Guid postId)
    {
        await commandMediator.SendAsync(new RestorePostCommand(postId));
        return NoContent();
    }

    [HttpDelete("{postId:guid}/destroy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([NotEmpty] Guid postId)
    {
        await commandMediator.SendAsync(new DeletePostCommand(postId));
        return NoContent();
    }

    [HttpDelete("recyclebin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Clear()
    {
        await commandMediator.SendAsync(new EmptyRecycleBinCommand());
        return NoContent();
    }
}
