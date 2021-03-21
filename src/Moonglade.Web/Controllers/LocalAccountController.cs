using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Auth;
using Moonglade.Web.Filters;
using Moonglade.Web.Models;
using Moonglade.Web.Models.Settings;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [ApiController]
    [AppendAppVersion]
    [Route("api/[controller]")]
    public class LocalAccountController : ControllerBase
    {
        private readonly ILocalAccountService _accountService;

        public LocalAccountController(ILocalAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(AccountEditViewModel model)
        {
            if (_accountService.Exist(model.Username))
            {
                ModelState.AddModelError("username", $"User '{model.Username}' already exist.");
                return Conflict(ModelState);
            }

            await _accountService.CreateAsync(model.Username, model.Password);
            return Ok();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty)
            {
                ModelState.AddModelError(nameof(id), "value is empty");
                return BadRequest(ModelState.CombineErrorMessages());
            }

            var uidClaim = User.Claims.FirstOrDefault(c => c.Type == "uid");
            if (null == uidClaim || string.IsNullOrWhiteSpace(uidClaim.Value))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Can not get current uid.");
            }

            if (id.ToString() == uidClaim.Value)
            {
                return Conflict("Can not delete current user.");
            }

            var count = _accountService.Count();
            if (count == 1)
            {
                return Conflict("Can not delete last account.");
            }

            await _accountService.DeleteAsync(id);
            return Ok();
        }

        [HttpPost("{id:guid}/reset-password")]
        public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordRequest request)
        {
            if (id == Guid.Empty)
            {
                ModelState.AddModelError(nameof(id), "value is empty");
                return BadRequest(ModelState.CombineErrorMessages());
            }

            if (!Regex.IsMatch(request.NewPassword, @"^(?=.*[A-Za-z])(?=.*\d)[!@#$%^&*A-Za-z\d]{8,}$"))
            {
                return Conflict("Password must be minimum eight characters, at least one letter and one number");
            }

            await _accountService.UpdatePasswordAsync(id, request.NewPassword);
            return Ok();
        }
    }
}
