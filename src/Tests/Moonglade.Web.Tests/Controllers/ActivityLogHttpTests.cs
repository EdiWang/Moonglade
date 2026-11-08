using Microsoft.AspNetCore.TestHost;
using Moonglade.ActivityLog;
using Moonglade.Web.Tests.TestSupport;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Moonglade.Web.Tests.Controllers;

public class ActivityLogHttpTests
{
    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("reader", HttpStatusCode.Forbidden)]
    public async Task Clear_AnonymousOrNonAdministrator_IsDeniedWithoutMutation(string? user, HttpStatusCode expected)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(ct: ct);
        using var client = fixture.App.GetTestClient();
        if (user != null) client.DefaultRequestHeaders.Add("X-B1-User", user);
        var original = await fixture.Store.ReadLogsAsync(ct);

        using var page = await client.GetAsync("/admin/activitylogs", ct);
        using var response = await client.DeleteAsync("/api/activitylog/clear", ct);

        Assert.Equal(expected, page.StatusCode);
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(original.Select(row => (row.Id, row.Operation, row.MetaData)),
            (await fixture.Store.ReadLogsAsync(ct)).Select(row => (row.Id, row.Operation, row.MetaData)));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("invalid")]
    public async Task Clear_AdminWithoutValidAntiforgery_IsDeniedWithoutMutation(string tokenMode)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(ct: ct);
        using var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        var original = await fixture.Store.ReadLogsAsync(ct);
        using var page = await client.GetAsync("/admin/activitylogs", ct);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        client.DefaultRequestHeaders.Add("Cookie", page.Headers.GetValues("Set-Cookie").Single().Split(';')[0]);
        if (tokenMode == "invalid") client.DefaultRequestHeaders.Add("XSRF-TOKEN", "not-a-valid-token");

        using var response = await client.DeleteAsync("/api/activitylog/clear", ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(original.Select(row => (row.Id, row.EventId, row.Operation)),
            (await fixture.Store.ReadLogsAsync(ct)).Select(row => (row.Id, row.EventId, row.Operation)));
    }

    [Fact]
    public async Task Clear_AdminWithPageToken_ClearsHistoryAndReturnsLegacyReadShape()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(ct: ct);
        using var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("b1-http-agent");
        using var history = JsonDocument.Parse(await client.GetStringAsync(
            $"/api/activitylog/list?pageIndex=1&pageSize=10&eventTypes={(int)EventType.CommentCreated}", ct));
        Assert.Equal(1, history.RootElement.GetProperty("totalItemCount").GetInt32());
        var historical = Assert.Single(history.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal("CommentCreated", historical.GetProperty("eventType").GetString());
        Assert.Equal("Historical operation 1", historical.GetProperty("operation").GetString());
        using var page = await client.GetAsync("/admin/activitylogs", ct);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var html = await page.Content.ReadAsStringAsync(ct);
        Assert.Contains("x-on:click=\"clearAllLogs()\"", html);
        Assert.Contains("data-confirm-clear-all-logs=", html);
        Assert.Contains("x-on:click=\"showDetail(log.id, log.operation)\"", html);
        Assert.Contains("/js/app/admin.activitylogs.mjs", html);
        var token = Regex.Match(html, "name=\"CSRF-TOKEN-MOONGLADE-FORM\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success, "Actual ActivityLogs Razor page must issue a form token.");
        client.DefaultRequestHeaders.Add("Cookie", page.Headers.GetValues("Set-Cookie").Single().Split(';')[0]);
        client.DefaultRequestHeaders.Add("XSRF-TOKEN", WebUtility.HtmlDecode(token.Groups[1].Value));

        using var response = await client.DeleteAsync("/api/activitylog/clear", ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync(ct));
        var row = Assert.Single(await fixture.Store.ReadLogsAsync(ct));
        Assert.Equal("admin", row.ActorId);
        Assert.Equal("192.0.2.25", row.IpAddress);
        Assert.Equal("b1-http-agent", row.UserAgent);
        using var metadata = JsonDocument.Parse(row.MetaData);
        Assert.Equal(3, metadata.RootElement.GetProperty("DeletedCount").GetInt32());
        using var list = JsonDocument.Parse(await client.GetStringAsync(
            $"/api/activitylog/list?pageIndex=1&pageSize=10&eventTypes={(int)EventType.ActivityLogsCleared}", ct));
        Assert.Equal(1, list.RootElement.GetProperty("totalItemCount").GetInt32());
        var item = Assert.Single(list.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal("ActivityLogsCleared", item.GetProperty("eventType").GetString());
        Assert.Equal("Clear Activity Logs", item.GetProperty("operation").GetString());
        Assert.Equal("Activity Logs", item.GetProperty("targetName").GetString());
        Assert.Equal("admin", item.GetProperty("actorId").GetString());
        Assert.Equal(row.Id, item.GetProperty("id").GetInt64());
    }
}
