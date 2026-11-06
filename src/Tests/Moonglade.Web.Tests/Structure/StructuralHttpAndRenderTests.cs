using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Web.Controllers;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Moonglade.Web.Tests.Structure;

public class StructuralHttpAndRenderTests
{
    [Theory]
    [InlineData(false, "{}", 0)]
    [InlineData(false, """{"feedItemCount":0,"useFullContent":false}""", 0)]
    [InlineData(false, """{"feedItemCount":5,"useFullContent":true}""", 5)]
    [InlineData(true, "", 0)]
    [InlineData(true, "FeedItemCount=5&UseFullContent=true", 5)]
    public async Task ImmutableInitProperties_BindJsonAndFormsWithoutChangingRequestDefaults(bool form, string body, int expected)
    {
        await using var app = await CreateApp();
        using var client = app.GetTestClient();
        using var response = await client.PostAsync(form ? "/s0/probe/feed-form" : "/s0/probe/feed",
            new StringContent(body, Encoding.UTF8, form ? "application/x-www-form-urlencoded" : "application/json"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await response.Content.ReadFromJsonAsync<ImmutableFeedProbe>(TestContext.Current.CancellationToken);
        Assert.Equal(expected, saved!.FeedItemCount);
    }

    [Theory]
    [InlineData(false, """{"feedItemCount":-1}""")]
    [InlineData(true, "FeedItemCount=-1")]
    public async Task ImmutableInitProperties_RetainAutomaticRangeValidation(bool form, string body)
    {
        await using var app = await CreateApp();
        using var client = app.GetTestClient();
        using var response = await client.PostAsync(form ? "/s0/probe/feed-form" : "/s0/probe/feed",
            new StringContent(body, Encoding.UTF8, form ? "application/x-www-form-urlencoded" : "application/json"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty(nameof(ImmutableFeedProbe.FeedItemCount), out _));
    }

    [Theory]
    [InlineData(false, null, true)]
    [InlineData(true, "null", false)]
    [InlineData(true, "[]", true)]
    [InlineData(true, "[null]", false)]
    [InlineData(true, """[{"title":"Parent","subMenus":[{"title":"Child","url":"/child"}]}]""", true)]
    [InlineData(true, """[{"title":"Parent","subMenus":[{"title":"Child","url":""}]}]""", false)]
    public async Task CurrentMenuEnvelope_CanBindAndValidateImmutableParsedGraph(bool enabled, string? json, bool valid)
    {
        await using var app = await CreateApp();
        using var client = app.GetTestClient();
        using var response = await client.PostAsJsonAsync("/s0/probe/menus", new CustomMenuSettingsJsonModel { IsEnabled = enabled, MenuJson = json },
            TestContext.Current.CancellationToken);
        Assert.Equal(valid ? HttpStatusCode.OK : HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RazorTagHelpers_ReadInitModelMetadataAndCurrentInputValue()
    {
        await using var app = await CreateApp();
        using var client = app.GetTestClient();
        var html = await client.GetStringAsync("/s0/probe/form", TestContext.Current.CancellationToken);
        Assert.Contains("Feed items</label>", html);
        Assert.Contains("name=\"FeedItemCount\"", html);
        Assert.Contains("value=\"0\"", html);
        Assert.Contains("data-val-range-min=\"0\"", html);
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(true, null)]
    [InlineData(false, "")]
    [InlineData(true, "")]
    [InlineData(false, "https://cdn.example.com/avatar.png")]
    [InlineData(true, "https://cdn.example.com/avatar.png")]
    public async Task Render_CapturedSnapshotFlowsThroughPartialComponentAndLayoutAcrossConcurrentPublication(bool razorPage, string? avatar)
    {
        await using var app = await CreateApp();
        using var client = app.GetTestClient();
        var store = app.Services.GetRequiredService<SnapshotProbeStore>();
        store.Publish(store.Capture() with { AvatarUrl = avatar });
        var original = store.Capture();
        var gate = app.Services.GetRequiredService<SnapshotRenderGate>();
        var route = razorPage ? "/s0/probe/page" : "/s0/probe/render";
        var pending = client.GetStringAsync(route, TestContext.Current.CancellationToken);
        var firstCompleted = await Task.WhenAny(gate.Started.Task, pending).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        if (firstCompleted == pending)
        {
            await pending;
            Assert.Fail("The probe render completed without reaching the partial-view gate.");
        }
        try
        {
            var nextMenu = original.Menu.Menus![0]! with { Title = "Changed" };
            store.Publish(original with
            {
                Feed = original.Feed with { FeedItemCount = 4 },
                Menu = original.Menu with { Menus = original.Menu.Menus.SetItem(0, nextMenu) }
            });
        }
        finally
        {
            gate.Continue.TrySetResult();
        }
        var first = await pending;
        foreach (var part in new[] { "root", "partial", "component", "layout" }) Assert.Contains($"{part}:20:About", first);
        Assert.Contains($"avatar:{(string.IsNullOrEmpty(avatar) ? "/blog/avatar" : avatar)}", first);
        Assert.Equal(avatar, original.AvatarUrl);
        Assert.Equal(avatar, store.Capture().AvatarUrl);

        var second = await client.GetStringAsync(route, TestContext.Current.CancellationToken);
        foreach (var part in new[] { "root", "partial", "component", "layout" }) Assert.Contains($"{part}:4:Changed", second);
    }

    [Fact]
    public async Task ANewOperationInTheSameRequest_CanCaptureNewState()
    {
        await using var app = await CreateApp();
        using var client = app.GetTestClient();
        var result = await client.GetFromJsonAsync<OperationProbeResult>("/s0/probe/operations", TestContext.Current.CancellationToken);
        Assert.Equal(new OperationProbeResult(20, 7), result);
    }

    private static async Task<WebApplication> CreateApp()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(StructuralProbeController).Assembly.GetName().Name,
            EnvironmentName = "Testing"
        });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton<SnapshotProbeStore>();
        builder.Services.AddSingleton<SnapshotRenderGate>();
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(AssetsController).Assembly)
            .ConfigureApplicationPartManager(manager =>
            {
                foreach (var part in manager.ApplicationParts.Where(part => part is CompiledRazorAssemblyPart
                    && part.Name != typeof(StructuralProbeController).Assembly.GetName().Name).ToArray())
                    manager.ApplicationParts.Remove(part);
                manager.FeatureProviders.Add(new ProbeControllersOnly());
            });
        builder.Services.AddRazorPages();
        var app = builder.Build();
        app.Use((context, next) =>
        {
            context.Request.PathBase = "/blog";
            return next(context);
        });
        app.MapControllers();
        app.MapRazorPages();
        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    private sealed class ProbeControllersOnly : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            foreach (var controller in feature.Controllers.Where(type => type.AsType() != typeof(StructuralProbeController)
                && type.AsType() != typeof(AssetsController)).ToArray()) feature.Controllers.Remove(controller);
        }
    }
}
