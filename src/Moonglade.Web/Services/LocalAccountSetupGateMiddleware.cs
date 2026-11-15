using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Moonglade.Web.Services;

public sealed class LocalAccountSetupGateMiddleware(RequestDelegate next)
{
    private volatile bool _setupComplete;

    public async Task InvokeAsync(HttpContext context, OwnerAccountStore accounts)
    {
        var path = context.Request.Path;
        if (path.Equals("/health", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/health/ready", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/auth/identity", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/auth/setup-account", StringComparison.OrdinalIgnoreCase)
            || await IsSetupCompleteAsync(accounts, context.RequestAborted))
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

    private async ValueTask<bool> IsSetupCompleteAsync(OwnerAccountStore accounts, CancellationToken ct)
    {
        // Setup only moves forward while the host is running. Recheck pending state for setup on other hosts.
        if (!_setupComplete && (await accounts.ReadAsync(ct))?.IsLocalInitialized == true)
        {
            _setupComplete = true;
        }

        return _setupComplete;
    }
}
