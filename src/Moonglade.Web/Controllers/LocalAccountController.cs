using Moonglade.Web.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LocalAccountController(IMediator mediator) : ControllerBase
{
    [HttpPut("{id:guid}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResetPassword([NotEmpty] Guid id, [FromBody][Required] string newPassword)
    {
        if (!Regex.IsMatch(newPassword, @"^(?=.*[A-Za-z])(?=.*\d)[!@#$%^&*A-Za-z\d]{8,}$"))
        {
            return Conflict("PasswordHash must be minimum eight characters, at least one letter and one number");
        }

        await mediator.Send(new ChangePasswordCommand(id, newPassword));
        return NoContent();
    }
}