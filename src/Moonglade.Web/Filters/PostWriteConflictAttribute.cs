using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moonglade.Data.Entities;

namespace Moonglade.Web.Filters;

public sealed class PostWriteConflictAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is not DbUpdateConcurrencyException { Entries.Count: > 0 } exception ||
            exception.Entries.Any(entry => entry.Entity is not PostEntity)) return;

        var services = context.HttpContext.RequestServices;
        var localizer = services.GetRequiredService<IStringLocalizer<Program>>();
        var factory = services.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateProblemDetails(context.HttpContext, StatusCodes.Status409Conflict,
            detail: localizer["The post changed. Reload the page to see its current state."]);
        context.Result = new ConflictObjectResult(problem) { ContentTypes = { "application/problem+json" } };
        context.ExceptionHandled = true;
    }
}
