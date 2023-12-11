using Moonglade.Web.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LocalAccountController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateAccountCommand command)
    {
        if (await mediator.Send(new AccountExistsQuery(command.Username)))
        {
            ModelState.AddModelError("username", $"User '{command.Username}' already exist.");
            return Conflict(ModelState);
        }

        await mediator.Send(command);
        return Ok();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete([NotEmpty] Guid id)
    {
        var uidClaim = User.Claims.FirstOrDefault(c => c.Type == "uid");
        if (null == uidClaim || string.IsNullOrWhiteSpace(uidClaim.Value))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Can not get current uid.");
        }

        if (id.ToString() == uidClaim.Value)
        {
            return Conflict("Can not delete current user.");
        }

        var count = await mediator.Send(new CountAccountsQuery());
        if (count == 1)
        {
            return Conflict("Can not delete last account.");
        }

        await mediator.Send(new DeleteAccountCommand(id));
        return NoContent();
    }

    [HttpPut("{id:guid}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResetPassword([NotEmpty] Guid id, [FromBody][Required] string newPassword)
    {
        if (!Regex.IsMatch(newPassword, @"^(?=.*[A-Za-z])(?=.*\d)[!@#$%^&*A-Za-z\d]{8,}$"))
        {
            return Conflict("Password must be minimum eight characters, at least one letter and one number");
        }

        await mediator.Send(new ChangePasswordCommand(id, newPassword));
        return NoContent();
    }
}