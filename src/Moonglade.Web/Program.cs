using Microsoft.Extensions.Logging.Console;
using Moonglade.Setup;
using Moonglade.Web.Extensions;
using System.Globalization;

namespace Moonglade.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        LoadAssemblies();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var cultures = GetSupportedCultures();
        var builder = WebApplication.CreateBuilder(args);
        builder.WriteParameterTable();

        if (builder.Environment.IsProduction())
        {
            builder.Logging.ClearProviders();
            builder.Logging.AddSimpleConsole(o =>
            {
                o.SingleLine = true;
                o.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
                o.UseUtcTimestamp = true;
                o.ColorBehavior = LoggerColorBehavior.Disabled;
            });

            builder.Logging.AddAzureWebAppDiagnostics();
        }

        builder.Services.AddMoongladeWebServices(builder.Configuration, cultures);

        var app = builder.Build();

        await app.InitStartUp();
        app.UseMoongladeRequestPipeline(cultures);
        app.MapMoongladeEndpoints();

        await app.RunAsync();
    }

    private static void LoadAssemblies()
    {
        var assemblies = new[]
        {
            "Moonglade.Webmention",
            "Moonglade.Auth",
            "Moonglade.Features",
            "Moonglade.Email.Client",
            "Moonglade.IndexNow.Client",
            "Moonglade.Syndication",
            "Moonglade.Theme",
            "Moonglade.Data",
            "Moonglade.Configuration",
            "Moonglade.Widgets",
            "Moonglade.ActivityLog"
        };

        foreach (var assembly in assemblies)
        {
            AppDomain.CurrentDomain.Load(assembly);
        }
    }

    private static List<CultureInfo> GetSupportedCultures()
    {
        var cultureCodes = new[] { "en-US", "zh-Hans", "zh-Hant", "de-DE", "ja-JP" };
        return [.. cultureCodes.Select(code => new CultureInfo(code))];
    }

}
