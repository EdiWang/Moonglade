using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moonglade.ActivityLog;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Web.Tests.TestSupport;
using System.Text.Json;

namespace Moonglade.Web.Tests.Controllers;

public partial class ActivityLogModelTests
{
    public static IEnumerable<object[]> ActivitySettingsGroups() => SettingsActivityCases.Groups.Select(group => new object[] { group.Group });

    [Theory]
    [MemberData(nameof(ActivitySettingsGroups))]
    public async Task SettingChanges_CurrentAndHistoricalFields_PersistTypedValues(string group)
    {
        var ct = TestContext.Current.CancellationToken;
        var expected = SettingsActivityCases.Groups.Single(item => item.Group == group);
        var historicalKeys = group switch
        {
            "general" => new[] { "UseDublinCoreMetaData", "UseGravatarAsProfilePicture" },
            "advanced" => new[] { "EnableSiteMap", "EnableJsonLdBlogPosting" },
            _ => []
        };
        Assert.Equal(expected.Allowed.Concat(historicalKeys).Order(), ActivityEvents.Get(expected.EventKey).SettingKeys!.Order());
        await using var store = await ActivityLogTestStore.CreateAsync(ct: ct);
        using var scope = store.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var model = SettingsActivityCases.Get(store.Config.Capture(), group);
        var changes = expected.Allowed.Concat(historicalKeys).Select(key =>
        {
            if (key == "UseGravatarAsProfilePicture") return new ActivityLogSettingChange(key, false, true);
            var owner = historicalKeys.Contains(key) ? store.Config.Capture().SEOSettings : model;
            var old = owner.GetType().GetProperty(key)!.GetValue(owner);
            object? value = old switch
            {
                bool boolean => !boolean,
                int number => number == 1 ? 2 : 1,
                Enum enumeration => Enum.GetValues(enumeration.GetType()).Cast<object>().First(item => !Equals(item, old)),
                _ => "After"
            };
            return new ActivityLogSettingChange(key, old, value);
        }).ToArray();

        await Writer(db, store).HandleAsync(new CreateActivityLogCommand(expected.EventKey, ActivityActorKind.Administrator, "admin",
            Details: ActivityLogDetails.Create(("Changes", changes))), ct);

        var row = Assert.Single(await store.ReadLogsAsync(ct));
        var result = (await new GetMetaDataByActivityLogIdQueryHandler(db).HandleAsync(new(row.Id), ct))!;
        var actual = result.Details["Changes"].EnumerateArray().ToArray();
        var typed = Assert.IsAssignableFrom<IReadOnlyList<ActivityLogSettingChangeDetail>>(result.SettingChanges);
        Assert.Equal(changes.Length, actual.Length);
        Assert.Equal(changes.Select(change => change.Key), typed.Select(change => change.Key));
        for (var index = 0; index < changes.Length; index++)
        {
            Assert.Equal(changes[index].Key, actual[index].GetProperty("Key").GetString());
            AssertScalar(changes[index].OldValue, actual[index].GetProperty("OldValue"));
            AssertScalar(changes[index].NewValue, actual[index].GetProperty("NewValue"));
            AssertScalar(changes[index].OldValue, typed[index].OldValue);
            AssertScalar(changes[index].NewValue, typed[index].NewValue);
        }
        Assert.Equal(["Changes"], result.Details.Keys);
        var list = Assert.IsAssignableFrom<IList<ActivityLogSettingChangeDetail>>(typed);
        Assert.Throws<NotSupportedException>(() => list.Clear());
        var serialized = JsonSerializer.SerializeToElement(result, JsonSerializerOptions.Web);
        Assert.False(serialized.TryGetProperty("settingChanges", out _));
        Assert.True(JsonElement.DeepEquals(result.Details["Changes"], serialized.GetProperty("details").GetProperty("Changes")));
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("null-to-empty")]
    [InlineData("empty-to-null")]
    [InlineData("controls")]
    [InlineData("bool")]
    [InlineData("integer")]
    [InlineData("enum")]
    public async Task SettingChanges_EmptyAndScalarBoundaries_RoundTripWithoutLosingTypes(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(ct: ct);
        using var scope = store.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        ActivityLogSettingChange[] changes = scenario switch
        {
            "empty" => [],
            "null-to-empty" => [new("SiteTitle", null, "")],
            "empty-to-null" => [new("SiteTitle", "", null)],
            "controls" => [new("SiteTitle", "Before", "<script>\u000C\u0001</script>")],
            "bool" => [new("WidgetsProfile", true, false)],
            "integer" => [new("HotTagAmount", 0, 20)],
            _ => [new("SideBarOption", SideBarOption.Right, SideBarOption.Disabled)]
        };

        await Writer(db, store).HandleAsync(new CreateActivityLogCommand(ActivityEvents.SettingsGeneralUpdated,
            ActivityActorKind.Administrator, "admin", Details: ActivityLogDetails.Create(("Changes", changes))), ct);

        var row = Assert.Single(await store.ReadLogsAsync(ct));
        var result = (await new GetMetaDataByActivityLogIdQueryHandler(db).HandleAsync(new(row.Id), ct))!;
        var actual = result.Details["Changes"].EnumerateArray().ToArray();
        var typed = Assert.IsAssignableFrom<IReadOnlyList<ActivityLogSettingChangeDetail>>(result.SettingChanges);
        Assert.Equal(changes.Length, actual.Length);
        Assert.Equal(changes.Length, typed.Count);
        if (scenario == "empty")
        {
            Assert.Equal("""{"Changes":[]}""", row.Details);
            Assert.Empty(typed);
        }
        else
        {
            var change = Assert.Single(actual);
            Assert.Equal(changes[0].Key, change.GetProperty("Key").GetString());
            AssertScalar(changes[0].OldValue, change.GetProperty("OldValue"));
            AssertScalar(changes[0].NewValue, change.GetProperty("NewValue"));
            var typedChange = Assert.Single(typed);
            Assert.Equal(changes[0].Key, typedChange.Key);
            AssertScalar(changes[0].OldValue, typedChange.OldValue);
            AssertScalar(changes[0].NewValue, typedChange.NewValue);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SettingChanges_OrdinaryDetails_HaveNoChangeEnvelopeAndPreserveFields(bool hasDetails)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(ct: ct);
        using var scope = store.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        await Writer(db, store).HandleAsync(new CreateActivityLogCommand(ActivityEvents.ActivityLogsCleared,
            ActivityActorKind.Administrator, "admin",
            Details: hasDetails ? ActivityLogDetails.Create(("DeletedCount", 3)) : null), ct);

        var row = Assert.Single(await store.ReadLogsAsync(ct));
        var result = (await new GetMetaDataByActivityLogIdQueryHandler(db).HandleAsync(new(row.Id), ct))!;

        Assert.Null(result.SettingChanges);
        if (hasDetails)
        {
            var field = Assert.Single(result.Details);
            Assert.Equal("DeletedCount", field.Key);
            Assert.Equal(JsonValueKind.Number, field.Value.ValueKind);
            Assert.Equal(3, field.Value.GetInt32());
        }
        else
        {
            Assert.Empty(result.Details);
        }
    }

    [Theory]
    [InlineData("restricted-key")]
    [InlineData("wrong-event-key")]
    [InlineData("wrong-case-key")]
    [InlineData("duplicate-key")]
    [InlineData("unchanged-value")]
    [InlineData("null-entry")]
    [InlineData("old-array")]
    [InlineData("new-array")]
    [InlineData("old-object")]
    [InlineData("new-object")]
    [InlineData("untyped-list")]
    [InlineData("null-envelope")]
    [InlineData("missing-envelope")]
    [InlineData("non-settings-event")]
    public async Task SettingChanges_InvalidWriterInput_RejectsBeforePersistenceAndLogsNoProtectedContents(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(ct: ct);
        using var scope = store.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        const string sentinel = "B3-PROTECTED-PAYLOAD";
        object? changes = scenario switch
        {
            "restricted-key" => new ActivityLogSettingChange[] { new("PasswordHash", "before", sentinel) },
            "wrong-event-key" => new ActivityLogSettingChange[] { new("FeedItemCount", 1, 2) },
            "wrong-case-key" => new ActivityLogSettingChange[] { new("siteTitle", "before", sentinel) },
            "duplicate-key" => new ActivityLogSettingChange[] { new("SiteTitle", "before", "after"), new("SiteTitle", "after", sentinel) },
            "unchanged-value" => new ActivityLogSettingChange[] { new("SiteTitle", sentinel, sentinel) },
            "null-entry" => new ActivityLogSettingChange[] { null! },
            "old-array" => new ActivityLogSettingChange[] { new("SiteTitle", new[] { sentinel }, "after") },
            "new-array" => new ActivityLogSettingChange[] { new("SiteTitle", "before", new[] { sentinel }) },
            "old-object" => new ActivityLogSettingChange[] { new("SiteTitle", new Dictionary<string, string> { ["private"] = sentinel }, "after") },
            "new-object" => new ActivityLogSettingChange[] { new("SiteTitle", "before", new Dictionary<string, string> { ["private"] = sentinel }) },
            "untyped-list" => new object[] { new Dictionary<string, object?> { ["Key"] = "SiteTitle", ["OldValue"] = "before", ["NewValue"] = sentinel } },
            "null-envelope" => null,
            _ => new ActivityLogSettingChange[] { new("SiteTitle", "before", sentinel) }
        };
        var unrelated = new BlogConfigurationEntity { CfgKey = "B3Unrelated", CfgValue = "retained", LastModifiedTimeUtc = DateTime.UtcNow };
        db.Add(unrelated);
        await Writer(db, store).HandleAsync(new CreateActivityLogCommand(
            scenario == "non-settings-event" ? ActivityEvents.PostCreated : ActivityEvents.SettingsGeneralUpdated,
            ActivityActorKind.Administrator, "admin", Details: scenario == "missing-envelope" ? null : ActivityLogDetails.Create(("Changes", changes))), ct);

        Assert.Empty(await store.ReadLogsAsync(ct));
        Assert.Empty(db.ChangeTracker.Entries<ActivityLogEntity>());
        Assert.Equal(EntityState.Added, db.Entry(unrelated).State);
        Assert.Empty(await db.BlogConfiguration.AsNoTracking().ToListAsync(ct));
        var diagnostic = Assert.Single(store.WriterLogger.Invocations);
        Assert.Equal(LogLevel.Error, diagnostic.Arguments[0]);
        Assert.Null(diagnostic.Arguments[3]);
        Assert.DoesNotContain(sentinel, diagnostic.Arguments[2]!.ToString());
        Assert.DoesNotContain("PasswordHash", diagnostic.Arguments[2]!.ToString());
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("null-root")]
    [InlineData("missing-envelope")]
    [InlineData("empty-root")]
    [InlineData("array-root")]
    [InlineData("unknown-root")]
    [InlineData("duplicate-root")]
    [InlineData("object-envelope")]
    [InlineData("null-envelope")]
    [InlineData("scalar-entry")]
    [InlineData("null-entry")]
    [InlineData("missing-key")]
    [InlineData("missing-old")]
    [InlineData("missing-new")]
    [InlineData("extra-member")]
    [InlineData("duplicate-member")]
    [InlineData("wrong-case-member")]
    [InlineData("unknown-key")]
    [InlineData("wrong-event-key")]
    [InlineData("duplicate-change")]
    [InlineData("same-values")]
    [InlineData("old-array")]
    [InlineData("new-object")]
    [InlineData("non-settings-event")]
    public async Task SettingChanges_InvalidStoredEnvelope_ThrowsSafeFailureBeforeReadExposure(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(ct: ct);
        using var scope = store.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        const string valid = """{"Key":"SiteTitle","OldValue":"before","NewValue":"B3-PROTECTED-PAYLOAD"}""";
        var json = scenario switch
        {
            "malformed" => """{"Changes":[B3-PROTECTED-PAYLOAD""",
            "null-root" => "null",
            "missing-envelope" => null,
            "empty-root" => "{}",
            "array-root" => "[]",
            "unknown-root" => """{"PasswordHash":"B3-PROTECTED-PAYLOAD"}""",
            "duplicate-root" => $$"""{"Changes":[{{valid}}],"Changes":[]}""",
            "object-envelope" => $$"""{"Changes":{{valid}}}""",
            "null-envelope" => """{"Changes":null}""",
            "scalar-entry" => """{"Changes":["B3-PROTECTED-PAYLOAD"]}""",
            "null-entry" => """{"Changes":[null]}""",
            "missing-key" => """{"Changes":[{"OldValue":"before","NewValue":"B3-PROTECTED-PAYLOAD"}]}""",
            "missing-old" => """{"Changes":[{"Key":"SiteTitle","NewValue":"B3-PROTECTED-PAYLOAD"}]}""",
            "missing-new" => """{"Changes":[{"Key":"SiteTitle","OldValue":"B3-PROTECTED-PAYLOAD"}]}""",
            "extra-member" => """{"Changes":[{"Key":"SiteTitle","OldValue":"before","NewValue":"after","PasswordHash":"B3-PROTECTED-PAYLOAD"}]}""",
            "duplicate-member" => """{"Changes":[{"Key":"SiteTitle","Key":"LogoText","NewValue":"B3-PROTECTED-PAYLOAD"}]}""",
            "wrong-case-member" => """{"Changes":[{"key":"SiteTitle","OldValue":"before","NewValue":"B3-PROTECTED-PAYLOAD"}]}""",
            "unknown-key" => """{"Changes":[{"Key":"PasswordHash","OldValue":"before","NewValue":"B3-PROTECTED-PAYLOAD"}]}""",
            "wrong-event-key" => """{"Changes":[{"Key":"FeedItemCount","OldValue":1,"NewValue":2}]}""",
            "duplicate-change" => $$"""{"Changes":[{{valid}},{{valid}}]}""",
            "same-values" => """{"Changes":[{"Key":"SiteTitle","OldValue":"B3-PROTECTED-PAYLOAD","NewValue":"B3-PROTECTED-PAYLOAD"}]}""",
            "old-array" => """{"Changes":[{"Key":"SiteTitle","OldValue":["B3-PROTECTED-PAYLOAD"],"NewValue":"after"}]}""",
            "new-object" => """{"Changes":[{"Key":"SiteTitle","OldValue":"before","NewValue":{"private":"B3-PROTECTED-PAYLOAD"}}]}""",
            _ => $$"""{"Changes":[{{valid}}]}"""
        };
        var row = new ActivityLogEntity
        {
            EventKey = scenario == "non-settings-event" ? ActivityEvents.PostCreated : ActivityEvents.SettingsGeneralUpdated,
            EventTimeUtc = DateTime.UtcNow,
            ActorKind = ActivityActorKind.Administrator,
            Details = json
        };
        db.Add(row);
        await db.SaveChangesAsync(ct);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
            new GetMetaDataByActivityLogIdQueryHandler(db).HandleAsync(new(row.Id), ct));

        Assert.DoesNotContain("B3-PROTECTED-PAYLOAD", exception.Message);
        Assert.DoesNotContain("PasswordHash", exception.Message);
        var listException = await Assert.ThrowsAsync<InvalidDataException>(() =>
            new ListActivityLogsQueryHandler(db).HandleAsync(new(), ct));
        Assert.DoesNotContain("B3-PROTECTED-PAYLOAD", listException.Message);
        Assert.DoesNotContain("PasswordHash", listException.Message);
        Assert.Equal(json, (await db.ActivityLog.AsNoTracking().SingleAsync(ct)).Details);
    }

    private static void AssertScalar(object? expected, JsonElement actual) =>
        Assert.True(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(expected is Enum ? expected.ToString() : expected), actual),
            "Stored scalar must equal the independently supplied value with its JSON type.");
}
