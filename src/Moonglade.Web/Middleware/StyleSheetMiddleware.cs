using System.Web;

namespace Moonglade.Web.Middleware;

public class StyleSheetMiddleware(RequestDelegate next)
{
    public static CustomCssMiddlewareOptions Options { get; set; } = new();

    public async Task Invoke(HttpContext context, IBlogConfig blogConfig, IMediator mediator)
    {
        var requestPath = context.Request.Path.ToString().ToLowerInvariant();

        if (!requestPath.EndsWith(".css"))
        {
            await next(context);
            return;
        }

        if (string.Equals(requestPath, Options.DefaultPath, StringComparison.OrdinalIgnoreCase))
        {
            await HandleDefaultPath(context, blogConfig);
        }
        else if (string.Equals(requestPath, "/content.css", StringComparison.OrdinalIgnoreCase) && context.Request.QueryString.HasValue)
        {
            await HandleContentCss(context, mediator);
        }
        else
        {
            await next(context);
        }
    }

    private async Task HandleDefaultPath(HttpContext context, IBlogConfig blogConfig)
    {
        if (!blogConfig.CustomStyleSheetSettings.EnableCustomCss)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var cssCode = blogConfig.CustomStyleSheetSettings.CssCode;
        await WriteStyleSheet(context, cssCode);
    }

    private async Task HandleContentCss(HttpContext context, IMediator mediator)
    {
        var qs = HttpUtility.ParseQueryString(context.Request.QueryString.Value!);
        var id = qs["id"];

        if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out var guid))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var css = await mediator.Send(new GetStyleSheetQuery(guid));
        if (css == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await WriteStyleSheet(context, css.CssContent);
    }

    private static async Task WriteStyleSheet(HttpContext context, string cssCode)
    {
        if (cssCode.Length > Options.MaxContentLength)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            return;
        }

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/css; charset=utf-8";
        await context.Response.WriteAsync(cssCode, context.RequestAborted);
    }
}

public static partial class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCustomCss(this IApplicationBuilder app, Action<CustomCssMiddlewareOptions> options)
    {
        options(StyleSheetMiddleware.Options);
        return app.UseMiddleware<StyleSheetMiddleware>();
    }
}

public class CustomCssMiddlewareOptions
{
    public int MaxContentLength { get; set; } = 65536;
    public PathString DefaultPath { get; set; } = "/custom.css";
}