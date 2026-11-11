using LiteBus.Events.Abstractions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Configuration;
using Moonglade.Email;
using Moonglade.Web.Tests.TestSupport;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Moonglade.Web.Tests.Controllers;

public partial class SettingsValidationHttpTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GeneralInput_CannotOverrideAccountProfile(bool patch)
    {
        var ct = TestContext.Current.CancellationToken;
        using var store = new ConfigurationCommandRecorder();
        var account = await store.Accounts.CreateProfileAsync("authoritative@example.test", ct);
        var config = Defaults();
        var before = config.Capture();
        await using var app = await CreateApp(store, config);
        using var client = app.GetTestClient();
        var submitted = JsonSerializer.SerializeToNode(before.GeneralSettings, new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();
        submitted["siteTitle"] = "New title";
        submitted["ownerName"] = "Attempted name";
        submitted["description"] = "Attempted description";
        submitted["ownerEmail"] = "attempt@example.test";
        if (patch)
        {
            using var invalid = await client.PatchAsJsonAsync("/api/settings/general", submitted, ct);
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            Assert.Equal(account, await store.Accounts.ReadAsync(ct));
            submitted.Remove("ownerEmail");
            submitted.Remove("ownerName");
            submitted.Remove("description");
        }

        using var response = patch
            ? await client.PatchAsJsonAsync("/api/settings/general", submitted, ct)
            : await client.PostAsJsonAsync("/api/settings/general", submitted, ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(account, await store.Accounts.ReadAsync(ct));
        Assert.Equal("New title", config.Capture().GeneralSettings.SiteTitle);
        Assert.Equal(Moonglade.Auth.OwnerPresentation.Default, await store.Accounts.ReadPresentationAsync(ct));
        Assert.Same(before.FeedSettings, config.Capture().FeedSettings);
        var json = await store.Db.BlogConfiguration.AsNoTracking().SingleAsync(row => row.CfgKey == nameof(GeneralSettings), ct);
        using var persisted = JsonDocument.Parse(json.CfgValue);
        Assert.Equal("New title", persisted.RootElement.GetProperty("siteTitle").GetString());
        Assert.DoesNotContain(persisted.RootElement.EnumerateObject(), property => property.Name.Equals("OwnerEmail", StringComparison.OrdinalIgnoreCase));
        var activity = Assert.Single(store.Commands.OfType<Moonglade.ActivityLog.CreateActivityLogCommand>());
        Assert.DoesNotContain("attempt@example.test", JsonSerializer.Serialize(activity.Details));
    }

    [Fact]
    public async Task TestEmail_MissingOwnerReturnsConflict_AndConfiguredOwnerIsSuppliedToNotification()
    {
        var ct = TestContext.Current.CancellationToken;
        using var store = new ConfigurationCommandRecorder();
        var config = Defaults();
        config.Publish(config.Capture() with
        { NotificationSettings = config.Capture().NotificationSettings with { EnableEmailSending = true } });
        await using var app = await CreateApp(store, config, emailAvailable: true);
        using var client = app.GetTestClient();
        var events = Mock.Get(app.Services.GetRequiredService<IEventMediator>());

        using var missing = await client.PostAsync("/api/settings/email/test", null, ct);

        Assert.Equal(HttpStatusCode.Conflict, missing.StatusCode);
        Assert.Empty(events.Invocations);
        var owner = await store.Accounts.CreateProfileAsync("configured@example.test", ct);
        using var configured = await client.PostAsync("/api/settings/email/test", null, ct);
        Assert.Equal(HttpStatusCode.OK, configured.StatusCode);
        var message = Assert.IsType<TestEmailEvent>(Assert.Single(events.Invocations).Arguments[0]);
        Assert.Equal(new Moonglade.Auth.OwnerProfile(owner.Id, owner.Email), message.Owner);
        Assert.Same(config.Capture(), message.Snapshot);
    }
}
