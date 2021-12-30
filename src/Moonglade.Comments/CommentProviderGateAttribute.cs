using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Configuration;

namespace Moonglade.Comments;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class CommentProviderGateAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var config = context.HttpContext.RequestServices.GetService<IBlogConfig>();

        if (config != null &&
            config.ContentSettings.EnableComments &&
            config.ContentSettings.CommentProvider == CommentProvider.BuiltIn)
        {
            await next().ConfigureAwait(false);
        }
        else
        {
            await HandleDisabledComment(context).ConfigureAwait(false);
        }
    }

    public Task HandleDisabledComment(ActionExecutingContext context)
    {
        context.Result = new StatusCodeResult(StatusCodes.Status404NotFound);
        return Task.CompletedTask;
    }
}