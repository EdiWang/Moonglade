using Microsoft.AspNetCore.Mvc.Filters;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Filters;

public sealed class SettingsValidationAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is not ValidationException exception) return;

        var validation = exception.ValidationResult ?? new ValidationResult("The settings are invalid.");
        var errors = validation.MemberNames.DefaultIfEmpty(string.Empty).Distinct()
            .ToDictionary(member => member, _ => new[] { validation.ErrorMessage });
        context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest
        });
        context.ExceptionHandled = true;
    }
}
