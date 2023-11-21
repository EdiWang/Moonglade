using NUglify;
using System.Web;

namespace Moonglade.Web.Middleware;

public class StyleSheetMiddleware(RequestDelegate next)
{
    public static CustomCssMiddlewareOptions Options { get; set; } = new();

    public async Task Invoke(HttpContext context, IBlogConfig blogConfig, IMediator mediator)
    {
        if (!context.Request.Path.ToString().ToLower().EndsWith(".css"))
        {
            await next(context);
            return;
        }

        if (context.Request.Path == Options.DefaultPath)
        {
            if (!blogConfig.CustomStyleSheetSettings.EnableCustomCss)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var cssCode = blogConfig.CustomStyleSheetSettings.CssCode;
            await WriteStyleSheet(context, cssCode);
        }
        else if (context.Request.Path == "/content.css" && context.Request.QueryString.HasValue)
        {
            // Get query string value
            var qs = HttpUtility.ParseQueryString(context.Request.QueryString.Value!);
            var id = qs["id"];

            if (!string.IsNullOrWhiteSpace(id))
            {
                if (!Guid.TryParse(id, out var guid))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                }
                else
                {
                    var css = await mediator.Send(new GetStyleSheetQuery(guid));
                    if (css == null)
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }

                    // TODO: May need a server side cache
                    await WriteStyleSheet(context, css.CssContent);
                }
            }
            else
            {
                await next(context);
            }
        }
        else
        {
            await next(context);
        }
    }

    private static async Task WriteStyleSheet(HttpContext context, string cssCode)
    {
        if (cssCode.Length > Options.MaxContentLength)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            return;
        }

        var uglifiedCss = Uglify.Css(cssCode);
        if (uglifiedCss.HasErrors)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            return;
        }

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/css; charset=utf-8";
        await context.Response.WriteAsync(uglifiedCss.Code, context.RequestAborted);
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