using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Rewrite;
using Moonglade.IndexNow.Client;
using Moonglade.Web.Handlers;
using System.Globalization;
using System.Net;

namespace Moonglade.Web.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseMoongladeRequestPipeline(this WebApplication app, List<CultureInfo> cultures)
    {
        bool useXFFHeaders = app.Configuration.GetValue<bool>("ForwardedHeaders:Enabled");
        if (useXFFHeaders) app.UseSmartXFFHeader();

        app.UseMiddleware<PrefersColorSchemeMiddleware>();
        app.UseMiddleware<PoweredByMiddleware>();

        app.UseExceptionHandler(ConfigureExceptionHandler.Handler);
        app.UseStatusCodePages(ProblemDetailsStatusCodePages.Handler);

        app.UseHttpsRedirection();
        app.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new("en-US"),
            SupportedCultures = cultures,
            SupportedUICultures = cultures
        });

        var options = new RewriteOptions().AddRedirect(@"(.*)/$", @"$1", (int)HttpStatusCode.MovedPermanently);
        app.UseRewriter(options);
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication().UseAuthorization();

        return app;
    }

    public static WebApplication MapMoongladeEndpoints(this WebApplication app)
    {
        app.MapStaticAssets();
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = PingEndpoint.WriteResponse,
            AllowCachingResponses = false
        });

        app.MapControllers();
        app.MapRazorPages().WithStaticAssets();
        app.MapStyleSheets(options => options.MaxContentLength = 10240);
        app.MapGet("/robots.txt", RobotsTxtMapHandler.Handler);

        if (!string.IsNullOrWhiteSpace(app.Configuration["IndexNow:ApiKey"]))
        {
            app.MapGet($"/{app.Configuration["IndexNow:ApiKey"]}.txt", IndexNowMapHandler.Handler);
        }

        app.MapGet("/manifest.webmanifest", WebManifestMapHandler.Handler);
        app.MapGet("/opensearch", OpenSearchMapHandler.Handler);

        var bc = app.Services.GetRequiredService<IBlogConfig>();
        if (bc.AdvancedSettings.EnableFoaf)
        {
            app.MapGet("/foaf.xml", FoafMapHandler.Handler);
        }

        if (bc.AdvancedSettings.EnableSiteMap)
        {
            app.MapGet("/sitemap.xml", SiteMapMapHandler.Handler);
        }

        return app;
    }
}