using NUglify;

namespace Moonglade.Web.Middleware;

public class CustomCssMiddleware
{
    private readonly RequestDelegate _next;

    public static CustomCssMiddlewareOptions Options { get; set; } = new();

    public CustomCssMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context, IBlogConfig blogConfig)
    {
        if (context.Request.Path == Options.DefaultPath)
        {
            if (!blogConfig.CustomStyleSheetSettings.EnableCustomCss)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var cssCode = blogConfig.CustomStyleSheetSettings.CssCode;
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
        else
        {
            await _next(context);
        }
    }
}

public static partial class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCustomCss(this IApplicationBuilder app, Action<CustomCssMiddlewareOptions> options)
    {
        options(CustomCssMiddleware.Options);
        return app.UseMiddleware<CustomCssMiddleware>();
    }
}

public class CustomCssMiddlewareOptions
{
    public int MaxContentLength { get; set; } = 65536;
    public PathString DefaultPath { get; set; } = "/custom.css";
}