using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using NUglify;

namespace Moonglade.Web.Middleware;

public class CustomCssMiddleware
{
    private readonly RequestDelegate _next;

    public static CustomCssMiddlewareOptions Options { get; set; } = new();

    public CustomCssMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context, IBlogConfig blogConfig)
    {
        if (!context.Request.Path.ToString().ToLower().EndsWith(".css"))
        {
            await _next(context);
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
        else if (context.Request.Path == "/page.css" && context.Request.QueryString.HasValue)
        {
            // Get query string value
            var qs = HttpUtility.ParseQueryString(context.Request.QueryString.Value!);
            string slug = qs["slug"];

            if (!string.IsNullOrWhiteSpace(slug))
            {
                slug = slug.ToLower();

                var slugRegex = "^(?!-)([a-zA-Z0-9-]){1,128}$";
                if (!Regex.IsMatch(slug, slugRegex, RegexOptions.Compiled, TimeSpan.FromSeconds(1)))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                }
                
                // TODO: Output blog page css
                // Need a server side cache
                // Need pattern validation
            }
            else
            {
                await _next(context);
            }
        }
        else
        {
            await _next(context);
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
        options(CustomCssMiddleware.Options);
        return app.UseMiddleware<CustomCssMiddleware>();
    }
}

public class CustomCssMiddlewareOptions
{
    public int MaxContentLength { get; set; } = 65536;
    public PathString DefaultPath { get; set; } = "/custom.css";
}