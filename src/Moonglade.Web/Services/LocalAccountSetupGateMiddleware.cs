using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Moonglade.Web.Services;

public sealed class LocalAccountSetupGateMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, OwnerAccountStore accounts)
    {
        var path = context.Request.Path;
        if (path.Equals("/health", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/health/ready", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/auth/identity", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/auth/setup-account", StringComparison.OrdinalIgnoreCase)
            || (await accounts.ReadAsync(context.RequestAborted))?.IsLocalInitialized == true)
        {
            await next(context);
            return;
        }
        context.Response.Headers.CacheControl = "no-store";
        if ((HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
            && context.GetEndpoint()?.Metadata.GetMetadata<PageActionDescriptor>() is not null)
        {
            context.Response.Redirect(context.Request.PathBase.Add("/auth/setup-account"));
            return;
        }
        context.Response.Headers.RetryAfter = "60";
        await Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Account setup is required.", detail: "The administrator account has not been configured.")
            .ExecuteAsync(context);
    }
}
