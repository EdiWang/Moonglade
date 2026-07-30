using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Web.Extensions;
using System.Globalization;
using System.Net;

namespace Moonglade.Web.Tests;

public class RequestPipelineRedirectTests
{
    private const string PlainTextResponse = """
        Moonglade response compression test payload. Moonglade response compression test payload.
        Moonglade response compression test payload. Moonglade response compression test payload.
        Moonglade response compression test payload. Moonglade response compression test payload.
        Moonglade response compression test payload. Moonglade response compression test payload.
        Moonglade response compression test payload. Moonglade response compression test payload.
        Moonglade response compression test payload. Moonglade response compression test payload.
        Moonglade response compression test payload. Moonglade response compression test payload.
        Moonglade response compression test payload. Moonglade response compression test payload.
        Moonglade response compression test payload. Moonglade response compression test payload.
        Moonglade response compression test payload. Moonglade response compression test payload.
        Moonglade response compression test payload. Moonglade response compression test payload.
        Moonglade response compression test payload. Moonglade response compression test payload.
        Moonglade response compression test payload. Moonglade response compression test payload.
        Moonglade response compression test payload. Moonglade response compression test payload.
        Moonglade response compression test payload. Moonglade response compression test payload.
        Moonglade response compression test payload. Moonglade response compression test payload.
        """;

    [Theory]
    [InlineData("/index.asp")]
    [InlineData("/index.aspx")]
    [InlineData("/index.htm")]
    [InlineData("/index.html")]
    [InlineData("/default.shtml")]
    [InlineData("/default.php")]
    [InlineData("/index.pl")]
    [InlineData("/default.cfm")]
    [InlineData("/Index.HTML")]
    public async Task RequestPipeline_WhenDefaultHomePageNameIsRequested_RedirectsToHome(string path)
    {
        using var app = await CreateTestApp();
        using var client = app.GetTestClient();
        client.BaseAddress = new Uri("https://localhost");

        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task RequestPipeline_WhenDefaultHomePageNameIsNested_DoesNotRedirectToHome()
    {
        using var app = await CreateTestApp();
        using var client = app.GetTestClient();
        client.BaseAddress = new Uri("https://localhost");

        var response = await client.GetAsync("/archive/index.html", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task RequestPipeline_WhenClientAcceptsGzip_CompressesTextResponse()
    {
        using var app = await CreateTestApp();
        using var client = app.GetTestClient();
        client.BaseAddress = new Uri("https://localhost");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/archive/index.html");
        request.Headers.AcceptEncoding.ParseAdd("gzip");

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("gzip", Assert.Single(response.Content.Headers.ContentEncoding));
    }

    private static async Task<WebApplication> CreateTestApp()
    {
        var webRoot = FindWebProjectRoot();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(Program).Assembly.GetName().Name,
            ContentRootPath = webRoot,
            WebRootPath = Path.Combine(webRoot, "wwwroot"),
            EnvironmentName = "Development"
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddProblemDetails();
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        builder.Services.AddRateLimiter();
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<GzipCompressionProvider>();
        });

        var app = builder.Build();
        app.UseMoongladeRequestPipeline(CreateCultures());
        app.Run(context =>
        {
            context.Response.ContentType = "text/plain; charset=utf-8";
            return context.Response.WriteAsync(PlainTextResponse, TestContext.Current.CancellationToken);
        });

        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    private static List<CultureInfo> CreateCultures() =>
    [
        new("en-US"),
        new("zh-Hans"),
        new("zh-Hant"),
        new("de-DE"),
        new("ja-JP")
    ];

    private static string FindWebProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "Moonglade.Web");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate src/Moonglade.Web.");
    }
}
