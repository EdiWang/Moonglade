using LiteBus.Events.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moq;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;

namespace Moonglade.Configuration.Tests;

public partial class UpdateConfigurationCommandTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegacyGeneralEmail_OrdinarySavePreservesAccountAndOtherValues_WithoutEmailAudit(bool patch)
    {
        var ct = TestContext.Current.CancellationToken;
        using var db = CreateDbContext();
        const string legacy = """{"ownerEmail":"legacy@example.test","ownerName":"Retained owner","siteTitle":"Before","description":"Retained description"}""";
        var parsed = BlogConfig.Parse(new Dictionary<string, string> { [nameof(GeneralSettings)] = legacy });
        var config = new BlogConfig(parsed.Snapshot);
        var account = new OwnerAccountEntity
        {
            Id = Guid.NewGuid(),
            Email = "authoritative@example.test",
            NormalizedEmail = "AUTHORITATIVE@EXAMPLE.TEST",
            Revision = Guid.NewGuid()
        };
        db.OwnerAccount.Add(account);
        var row = await SeedGroup(db, config.Capture().GeneralSettings, ct);
        var submitted = BlogConfig.Parse(new Dictionary<string, string>
        {
            [nameof(GeneralSettings)] = """{"ownerEmail":"attempt@example.test","ownerName":"Retained owner","siteTitle":"After","description":"Retained description"}"""
        }).Snapshot.GeneralSettings;

        var result = await Save(Handler(db, config), submitted, patch, ["SiteTitle"], ct);

        Assert.Equal("After", config.Capture().GeneralSettings.SiteTitle);
        Assert.Equal(new SettingValueChange("SiteTitle", "Before", "After"), Assert.Single(result.Changes));
        Assert.Equal("authoritative@example.test", (await db.OwnerAccount.AsNoTracking().SingleAsync(ct)).Email);
        Assert.Equal(account.Revision, (await db.OwnerAccount.AsNoTracking().SingleAsync(ct)).Revision);
        using var json = JsonDocument.Parse(row.CfgValue);
        Assert.DoesNotContain(json.RootElement.EnumerateObject(), value => value.Name.Equals("OwnerEmail", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("After", json.RootElement.GetProperty("siteTitle").GetString());
    }

    public static IEnumerable<object[]> OrdinaryFieldCases()
    {
        foreach (var (group, _) in SettingChangeCases.Allowed)
            foreach (var property in SettingChangeCases.Group(SettingChangeCases.Baseline(), group).GetType()
                         .GetProperties(BindingFlags.Instance | BindingFlags.Public))
                foreach (var patch in new[] { false, true })
                    yield return [group, property.Name, patch];
    }

    [Theory]
    [MemberData(nameof(OrdinaryFieldCases))]
    public async Task Save_AllOrdinaryFields_ProjectsOnlyExactB0AllowlistedChanges(string group, string key, bool patch)
    {
        var ct = TestContext.Current.CancellationToken;
        using var db = CreateDbContext();
        var original = SettingChangeCases.Baseline();
        var config = new BlogConfig(original);
        var before = SettingChangeCases.Group(original, group);
        var retainedJson = before.ToJson();
        var after = SettingChangeCases.Change(before, key);
        var row = await SeedGroup(db, before, ct);
        var timestamp = row.LastModifiedTimeUtc;

        var result = await Save(Handler(db, config), after, patch, [key], ct);

        Assert.Equal(before.GetType(), result.SettingsType);
        Assert.True(result.HasEffectiveChanges);
        Assert.Equal(after.ToJson(), SettingChangeCases.Group(config.Capture(), group).ToJson());
        Assert.Equal(retainedJson, SettingChangeCases.Group(original, group).ToJson());
        Assert.Equal(after.ToJson(), row.CfgValue);
        Assert.True(row.LastModifiedTimeUtc > timestamp);
        if (SettingChangeCases.Allowed[group].Contains(key))
        {
            var change = Assert.Single(result.Changes);
            Assert.Equal(key, change.Key);
            Assert.Equal(before.GetType().GetProperty(key)!.GetValue(before), change.OldValue);
            Assert.Equal(after.GetType().GetProperty(key)!.GetValue(after), change.NewValue);
            Assert.NotEqual(change.OldValue, change.NewValue);
        }
        else
        {
            Assert.Empty(result.Changes);
            Assert.DoesNotContain("B3-private", result.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        if (result.Changes is IList<SettingValueChange> list)
            Assert.Throws<NotSupportedException>(() => list.Add(new("SiteTitle", "unsafe", "unsafe")));
    }

    [Theory]
    [InlineData(nameof(GeneralSettings))]
    [InlineData(nameof(ContentSettings))]
    [InlineData(nameof(CommentSettings))]
    [InlineData(nameof(NotificationSettings))]
    [InlineData(nameof(FeedSettings))]
    [InlineData(nameof(ImageSettings))]
    [InlineData(nameof(AdvancedSettings))]
    [InlineData(nameof(AppearanceSettings))]
    [InlineData(nameof(CustomMenuSettings))]
    public async Task UnchangedSave_AllNineGroups_ReturnsNoEffectiveChangesAndRefreshesRoot(string group)
    {
        var ct = TestContext.Current.CancellationToken;
        using var db = CreateDbContext();
        var original = SettingChangeCases.Baseline();
        var config = new BlogConfig(original);
        var before = SettingChangeCases.Group(original, group);
        var row = await SeedGroup(db, before, ct);
        var time = row.LastModifiedTimeUtc;
        var events = new Mock<IEventMediator>();
        var handler = new UpdateConfigurationCommandHandler(db, config, events.Object, _gate,
            NullLogger<UpdateConfigurationCommandHandler>.Instance);

        foreach (var patch in new[] { false, true })
        {
            var root = config.Capture();
            var result = await Save(handler, SettingChangeCases.Copy(before), patch, [SettingChangeCases.Allowed[group][0]], ct);
            Assert.False(result.HasEffectiveChanges);
            Assert.Empty(result.Changes);
            Assert.Equal(before.GetType(), result.SettingsType);
            Assert.NotSame(root, config.Capture());
            Assert.Equal(before.ToJson(), row.CfgValue);
            Assert.Equal(time, row.LastModifiedTimeUtc);
            Assert.Empty(events.Invocations);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MixedSave_AllGroups_ProjectOnlyChangedSafeValues(bool patch)
    {
        var ct = TestContext.Current.CancellationToken;
        foreach (var (group, allowed) in SettingChangeCases.Allowed)
        {
            using var db = CreateDbContext();
            var root = SettingChangeCases.Baseline();
            var before = SettingChangeCases.Group(root, group);
            var after = SettingChangeCases.Copy(before);
            var keys = before.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).ToArray();
            foreach (var key in keys)
                before.GetType().GetProperty(key)!.SetValue(after,
                    before.GetType().GetProperty(key)!.GetValue(SettingChangeCases.Change(before, key)));
            var config = new BlogConfig(root);
            await SeedGroup(db, before, ct);

            var result = await Save(Handler(db, config), after, patch, keys, ct);

            Assert.True(result.HasEffectiveChanges);
            Assert.Equal(allowed.Order(), result.Changes.Select(change => change.Key).Order());
            foreach (var change in result.Changes)
            {
                Assert.Equal(before.GetType().GetProperty(change.Key)!.GetValue(before), change.OldValue);
                Assert.Equal(after.GetType().GetProperty(change.Key)!.GetValue(after), change.NewValue);
            }
            Assert.DoesNotContain("B3-private", JsonSerializer.Serialize(result.Changes), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("before.example.invalid", JsonSerializer.Serialize(result.Changes));
            Assert.Equal(after.ToJson(), SettingChangeCases.Group(config.Capture(), group).ToJson());
        }
    }

    [Theory]
    [InlineData("case", false)]
    [InlineData("order", false)]
    [InlineData("omitted", false)]
    [InlineData("case", true)]
    [InlineData("order", true)]
    [InlineData("omitted", true)]
    public async Task LegacyEquivalentJson_NormalizesRawStorageAndCacheWithoutEffectiveAudit(string variant, bool patch)
    {
        var ct = TestContext.Current.CancellationToken;
        using var db = CreateDbContext();
        var feed = new FeedSettings { FeedItemCount = 20, UseFullContent = false };
        var json = variant switch
        {
            "case" => """{"FEEDITEMCOUNT":20,"USEFULLCONTENT":false}""",
            "order" => """{"useFullContent":false,"feedItemCount":20}""",
            _ => "{}"
        };
        var config = new BlogConfig(BlogConfig.Parse(new Dictionary<string, string> { [nameof(FeedSettings)] = json }).Snapshot);
        var row = await SeedGroup(db, feed, ct);
        row.CfgValue = json;
        await db.SaveChangesAsync(ct);
        var time = row.LastModifiedTimeUtc;
        var events = new Mock<IEventMediator>();
        var handler = new UpdateConfigurationCommandHandler(db, config, events.Object, _gate,
            NullLogger<UpdateConfigurationCommandHandler>.Instance);

        var result = await Save(handler, feed, patch, [nameof(FeedSettings.FeedItemCount)], ct);

        Assert.False(result.HasEffectiveChanges);
        Assert.Empty(result.Changes);
        Assert.Equal(20, config.Capture().FeedSettings.FeedItemCount);
        Assert.False(config.Capture().FeedSettings.UseFullContent);
        Assert.Equal(feed.ToJson(), row.CfgValue);
        Assert.True(row.LastModifiedTimeUtc > time);
        var message = Assert.IsType<ConfigurationChangedEvent>(Assert.Single(events.Invocations).Arguments[0]);
        Assert.Equal([nameof(FeedSettings)], message.Names);
    }

    [Fact]
    public async Task RawUnchangedButEffectiveDifferent_UsesCapturedCurrentValuesWithoutCacheEvent()
    {
        var ct = TestContext.Current.CancellationToken;
        using var db = CreateDbContext();
        var config = new BlogConfig(new BlogSettingsSnapshot { GeneralSettings = General("Old") });
        var submitted = General("New");
        var row = await SeedGroup(db, submitted, ct);
        var time = row.LastModifiedTimeUtc;
        var events = new Mock<IEventMediator>();
        var result = await new UpdateConfigurationCommandHandler(db, config, events.Object, _gate,
            NullLogger<UpdateConfigurationCommandHandler>.Instance).HandleAsync(UpdateConfigurationCommand.Create(submitted), ct);

        Assert.True(result.HasEffectiveChanges);
        Assert.Equal(new SettingValueChange("SiteTitle", "Old", "New"), Assert.Single(result.Changes));
        Assert.Equal(time, row.LastModifiedTimeUtc);
        Assert.Empty(events.Invocations);
        Assert.Equal("New", config.Capture().GeneralSettings.SiteTitle);
    }

    [Theory]
    [InlineData("equivalent", false)]
    [InlineData("parent-title", true)]
    [InlineData("parent-url", true)]
    [InlineData("parent-icon", true)]
    [InlineData("parent-order", true)]
    [InlineData("parent-tab", true)]
    [InlineData("child-title", true)]
    [InlineData("child-url", true)]
    [InlineData("child-tab", true)]
    [InlineData("child-add", true)]
    [InlineData("child-remove", true)]
    [InlineData("child-reorder", true)]
    [InlineData("parent-add", true)]
    [InlineData("parent-remove", true)]
    [InlineData("parent-reorder", true)]
    [InlineData("enabled", true)]
    public async Task NestedMenus_CompareStructurallyAndExposeOnlyEnableFlag(string variant, bool changed)
    {
        var ct = TestContext.Current.CancellationToken;
        using var db = CreateDbContext();
        var before = new CustomMenuSettings
        {
            IsEnabled = true,
            Menus =
            [
                new Menu { Title = "Parent one", Url = "/one", Icon = "bi-star", DisplayOrder = 1,
                    SubMenus = [new SubMenu { Title = "Child one", Url = "/child-one" }, new SubMenu { Title = "Child two", Url = "/child-two" }] },
                new Menu { Title = "Parent two", Url = "/two", DisplayOrder = 2 }
            ]
        };
        var menus = before.Menus.Select(menu => menu with { SubMenus = menu.SubMenus.Select(child => child with { }).ToImmutableList() }).ToImmutableList();
        var first = menus[0];
        first = variant switch
        {
            "parent-title" => first with { Title = "Private changed title" },
            "parent-url" => first with { Url = "/changed" },
            "parent-icon" => first with { Icon = "bi-book" },
            "parent-order" => first with { DisplayOrder = 9 },
            "parent-tab" => first with { IsOpenInNewTab = true },
            "child-title" => first with { SubMenus = first.SubMenus.SetItem(0, first.SubMenus[0] with { Title = "Changed child" }) },
            "child-url" => first with { SubMenus = first.SubMenus.SetItem(0, first.SubMenus[0] with { Url = "/changed-child" }) },
            "child-tab" => first with { SubMenus = first.SubMenus.SetItem(0, first.SubMenus[0] with { IsOpenInNewTab = true }) },
            "child-add" => first with { SubMenus = first.SubMenus.Add(new SubMenu { Title = "Extra", Url = "/extra" }) },
            "child-remove" => first with { SubMenus = first.SubMenus.RemoveAt(0) },
            "child-reorder" => first with { SubMenus = first.SubMenus.Reverse().ToImmutableList() },
            _ => first
        };
        menus = menus.SetItem(0, first);
        menus = variant switch
        {
            "parent-add" => menus.Add(new Menu { Title = "Extra" }),
            "parent-remove" => menus.RemoveAt(0),
            "parent-reorder" => menus.Reverse().ToImmutableList(),
            _ => menus
        };
        var after = before with { IsEnabled = variant != "enabled", Menus = menus };
        var config = new BlogConfig(new BlogSettingsSnapshot { CustomMenuSettings = before });
        var row = await SeedGroup(db, before, ct);
        var time = row.LastModifiedTimeUtc;

        var result = await Handler(db, config).HandleAsync(UpdateConfigurationCommand.Create(after), ct);

        Assert.Equal(changed, result.HasEffectiveChanges);
        if (variant == "enabled")
            Assert.Equal(new SettingValueChange("IsEnabled", true, false), Assert.Single(result.Changes));
        else Assert.Empty(result.Changes);
        Assert.Equal(after.ToJson(), row.CfgValue);
        Assert.Equal(changed, row.LastModifiedTimeUtc > time);
        Assert.Equal("Parent one", before.Menus[0].Title);
        Assert.Equal("/child-one", before.Menus[0].SubMenus[0].Url);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Save_ResultWaitsForRuntimeAndCacheCompletion(bool patch)
    {
        var ct = TestContext.Current.CancellationToken;
        using var db = CreateDbContext();
        var config = new BlogConfig(new BlogSettingsSnapshot { GeneralSettings = General("Old") });
        var retained = config.Capture();
        await SeedGroup(db, retained.GeneralSettings, ct);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var events = new Mock<IEventMediator>();
        events.Setup(m => m.PublishAsync(It.IsAny<IEvent>(), It.IsAny<EventMediationSettings>(), CancellationToken.None))
            .Callback(() => entered.SetResult()).Returns(release.Task);
        var handler = new UpdateConfigurationCommandHandler(db, config, events.Object, _gate,
            NullLogger<UpdateConfigurationCommandHandler>.Instance);
        var pending = Save(handler, General("New"), patch, [nameof(GeneralSettings.SiteTitle)], ct);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
        try
        {
            Assert.False(pending.IsCompleted);
            Assert.Equal("New", config.Capture().GeneralSettings.SiteTitle);
            Assert.Equal("Old", retained.GeneralSettings.SiteTitle);
            Assert.Equal("New", db.BlogConfiguration.Local.Single().CfgValue.FromJson<GeneralSettings>().SiteTitle);
        }
        finally { release.TrySetResult(); }
        var result = await pending;
        Assert.Equal(new SettingValueChange("SiteTitle", "Old", "New"), Assert.Single(result.Changes));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CacheFailureAfterCommit_ThrowsInsteadOfReturningSuccessAndKeepsSavedState(bool patch)
    {
        var ct = TestContext.Current.CancellationToken;
        using var db = CreateDbContext();
        var config = new BlogConfig(new BlogSettingsSnapshot { GeneralSettings = General("Old") });
        var row = await SeedGroup(db, config.Capture().GeneralSettings, ct);
        var time = row.LastModifiedTimeUtc;
        var events = new Mock<IEventMediator>();
        var failure = new InvalidOperationException("Synthetic cache failure");
        events.Setup(m => m.PublishAsync(It.IsAny<IEvent>(), It.IsAny<EventMediationSettings>(), CancellationToken.None)).ThrowsAsync(failure);
        var handler = new UpdateConfigurationCommandHandler(db, config, events.Object, _gate,
            NullLogger<UpdateConfigurationCommandHandler>.Instance);

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => Save(handler, General("New"), patch, ["SiteTitle"], ct)));
        Assert.Equal("New", config.Capture().GeneralSettings.SiteTitle);
        Assert.Equal("New", row.CfgValue.FromJson<GeneralSettings>().SiteTitle);
        Assert.True(row.LastModifiedTimeUtc > time);
    }

    [Theory]
    [InlineData("patch-patch")]
    [InlineData("full-patch")]
    [InlineData("patch-full")]
    public async Task OverlappingSameFieldSaves_ReturnTheirActualGatedBeforeAndAfter(string variant)
    {
        var ct = TestContext.Current.CancellationToken;
        var interceptor = new BlockingSaveInterceptor();
        var options = new DbContextOptionsBuilder<BlogDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor).Options;
        using var firstDb = new BlogDbContext(options);
        using var secondDb = new BlogDbContext(options);
        var config = new BlogConfig(new BlogSettingsSnapshot { GeneralSettings = General("Old") });
        await SeedGroup(firstDb, config.Capture().GeneralSettings, ct);
        interceptor.Enabled = true;
        var first = Save(Handler(firstDb, config), General("First"), variant != "full-patch", ["SiteTitle"], ct);
        await interceptor.Committed.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
        var second = Save(Handler(secondDb, config), General("Second"), variant != "patch-full", ["SiteTitle"], ct);
        try { Assert.False(second.IsCompleted); }
        finally { interceptor.Resume.TrySetResult(); }
        await Task.WhenAll(first, second);

        Assert.Equal(new SettingValueChange("SiteTitle", "Old", "First"), Assert.Single(first.Result.Changes));
        Assert.Equal(new SettingValueChange("SiteTitle", "First", "Second"), Assert.Single(second.Result.Changes));
        Assert.Equal("Second", config.Capture().GeneralSettings.SiteTitle);
        Assert.Equal("Second", (await secondDb.BlogConfiguration.AsNoTracking().SingleAsync(ct)).CfgValue.FromJson<GeneralSettings>().SiteTitle);
    }

    [Fact]
    public async Task OverlappingDifferentGroupPatches_ReturnOwnValuesAndRetainLatestOtherGroup()
    {
        var ct = TestContext.Current.CancellationToken;
        var interceptor = new BlockingSaveInterceptor();
        var options = new DbContextOptionsBuilder<BlogDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor).Options;
        using var firstDb = new BlogDbContext(options);
        using var secondDb = new BlogDbContext(options);
        var original = new BlogSettingsSnapshot { GeneralSettings = General("Old"), FeedSettings = FeedSettings.DefaultValue };
        var config = new BlogConfig(original);
        await SeedGroup(firstDb, original.GeneralSettings, ct);
        await SeedGroup(firstDb, original.FeedSettings, ct);
        interceptor.Enabled = true;
        var first = Save(Handler(firstDb, config), General("New"), true, ["SiteTitle"], ct);
        await interceptor.Committed.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
        var second = Save(Handler(secondDb, config), new FeedSettings { FeedItemCount = 7 }, true, ["FeedItemCount"], ct);
        try { Assert.False(second.IsCompleted); }
        finally { interceptor.Resume.TrySetResult(); }
        await Task.WhenAll(first, second);

        Assert.Equal(new SettingValueChange("SiteTitle", "Old", "New"), Assert.Single(first.Result.Changes));
        Assert.Equal(new SettingValueChange("FeedItemCount", 20, 7), Assert.Single(second.Result.Changes));
        Assert.Equal("New", config.Capture().GeneralSettings.SiteTitle);
        Assert.Equal(7, config.Capture().FeedSettings.FeedItemCount);
        Assert.Equal("Old", original.GeneralSettings.SiteTitle);
        Assert.Equal(20, original.FeedSettings.FeedItemCount);
    }

    [Theory]
    [InlineData("validation")]
    [InlineData("missing")]
    [InlineData("persistence")]
    [InlineData("cancellation")]
    public async Task Patch_RejectedBeforeCommit_PreservesRootRowsTimestampsAndCache(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        var interceptor = new FailingSaveInterceptor();
        using var db = CreateDbContext(interceptor);
        var config = new BlogConfig(new BlogSettingsSnapshot { FeedSettings = FeedSettings.DefaultValue });
        var original = config.Capture();
        if (scenario != "missing") await SeedGroup(db, original.FeedSettings, ct);
        interceptor.Fail = scenario == "persistence";
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (scenario == "cancellation") await cancellation.CancelAsync();
        var events = new Mock<IEventMediator>();
        var handler = new UpdateConfigurationCommandHandler(db, config, events.Object, _gate,
            NullLogger<UpdateConfigurationCommandHandler>.Instance);
        var value = new FeedSettings { FeedItemCount = scenario == "validation" ? -1 : 7 };

        var error = await Record.ExceptionAsync(() => Save(handler, value, true, ["FeedItemCount"], cancellation.Token));
        switch (scenario)
        {
            case "validation": Assert.IsType<ValidationException>(error); break;
            case "missing": Assert.IsType<InvalidOperationException>(error); break;
            case "persistence": Assert.IsType<DbUpdateException>(error); break;
            default: Assert.IsAssignableFrom<OperationCanceledException>(error); break;
        }
        Assert.Same(original, config.Capture());
        Assert.Empty(events.Invocations);
        db.ChangeTracker.Clear();
        if (scenario == "missing") Assert.Empty(await db.BlogConfiguration.ToListAsync(ct));
        else
        {
            var row = await db.BlogConfiguration.SingleAsync(ct);
            Assert.Equal(original.FeedSettings.ToJson(), row.CfgValue);
            Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), row.LastModifiedTimeUtc);
        }
    }

    [Theory]
    [InlineData("null-to-empty")]
    [InlineData("empty-to-null")]
    [InlineData("null-to-null")]
    [InlineData("empty-to-empty")]
    public async Task DisabledMenu_NullAndEmptyPartitionsAreComparedWithoutExposingGraph(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        using var db = CreateDbContext();
        var before = new CustomMenuSettings { IsEnabled = false, Menus = scenario.StartsWith("null", StringComparison.Ordinal) ? null! : [] };
        var after = before with { Menus = scenario.EndsWith("null", StringComparison.Ordinal) ? null! : [] };
        var config = new BlogConfig(new BlogSettingsSnapshot { CustomMenuSettings = before });
        await SeedGroup(db, before, ct);

        var result = await Handler(db, config).HandleAsync(UpdateConfigurationCommand.Create(after), ct);

        Assert.Equal(scenario is "null-to-empty" or "empty-to-null", result.HasEffectiveChanges);
        Assert.Empty(result.Changes);
        Assert.False(config.Capture().CustomMenuSettings.IsEnabled);
        Assert.Equal(after.ToJson(), db.BlogConfiguration.Local.Single().CfgValue);
    }

    [Theory]
    [InlineData("null-to-empty")]
    [InlineData("empty-to-null")]
    [InlineData("string-controls")]
    [InlineData("feed-zero")]
    [InlineData("feed-max")]
    [InlineData("bool-false")]
    [InlineData("enum")]
    [InlineData("title-max")]
    public async Task ApprovedScalarBoundaries_PreserveValuesAndRuntimeWithoutReportDataInNames(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        using var db = CreateDbContext();
        object before = scenario switch
        {
            "null-to-empty" => ContentSettings.DefaultValue with { CalloutSectionHtmlPitch = null! },
            "empty-to-null" => ContentSettings.DefaultValue,
            "string-controls" => ContentSettings.DefaultValue,
            "feed-zero" or "feed-max" => FeedSettings.DefaultValue,
            "bool-false" => CommentSettings.DefaultValue,
            "enum" => General("Old") with { SideBarOption = SideBarOption.Right },
            _ => General("Old")
        };
        object after = scenario switch
        {
            "null-to-empty" => ((ContentSettings)before) with { CalloutSectionHtmlPitch = "" },
            "empty-to-null" => ((ContentSettings)before) with { CalloutSectionHtmlPitch = null! },
            "string-controls" => ((ContentSettings)before) with { CalloutSectionHtmlPitch = "private\u000C\u0001<text>" },
            "feed-zero" => new FeedSettings { FeedItemCount = 0 },
            "feed-max" => new FeedSettings { FeedItemCount = int.MaxValue },
            "bool-false" => ((CommentSettings)before) with { EnableComments = false },
            "enum" => ((GeneralSettings)before) with { SideBarOption = SideBarOption.Disabled },
            _ => ((GeneralSettings)before) with { SiteTitle = new string('x', 16) }
        };
        var root = SettingChangeCases.Baseline();
        typeof(BlogSettingsSnapshot).GetProperty(before.GetType().Name)!.SetValue(root, before);
        var config = new BlogConfig(root);
        await SeedGroup(db, before, ct);

        var result = await Handler(db, config).HandleAsync(FullCommand(after), ct);

        Assert.True(result.HasEffectiveChanges);
        if (scenario is "null-to-empty" or "empty-to-null" or "string-controls")
            Assert.Empty(result.Changes);
        else
        {
            var expected = scenario switch
            {
                "feed-zero" => new SettingValueChange("FeedItemCount", 20, 0),
                "feed-max" => new SettingValueChange("FeedItemCount", 20, int.MaxValue),
                "bool-false" => new SettingValueChange("EnableComments", true, false),
                "enum" => new SettingValueChange("SideBarOption", SideBarOption.Right, SideBarOption.Disabled),
                _ => new SettingValueChange("SiteTitle", "Old", new string('x', 16))
            };
            Assert.Equal(expected, Assert.Single(result.Changes));
        }
        Assert.Equal(after.ToJson(), SettingChangeCases.Group(config.Capture(), after.GetType().Name).ToJson());
        Assert.Equal(after.ToJson(), db.BlogConfiguration.Local.Single().CfgValue);
    }

    private static UpdateConfigurationCommand FullCommand(object value) =>
        (UpdateConfigurationCommand)typeof(UpdateConfigurationCommand).GetMethod(nameof(UpdateConfigurationCommand.Create))!
            .MakeGenericMethod(value.GetType()).Invoke(null, [value])!;

    private static async Task<BlogConfigurationEntity> SeedGroup(BlogDbContext db, object value, CancellationToken ct)
    {
        var row = new BlogConfigurationEntity
        {
            CfgKey = value.GetType().Name,
            CfgValue = value.ToJson(),
            LastModifiedTimeUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        db.BlogConfiguration.Add(row);
        await db.SaveChangesAsync(ct);
        return row;
    }

    private static Task<ConfigurationSaveResult> Save(UpdateConfigurationCommandHandler handler, object value, bool patch,
        string[] fields, CancellationToken ct)
    {
        var method = typeof(UpdateConfigurationCommandTests).GetMethod(nameof(SaveTyped), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(value.GetType());
        return (Task<ConfigurationSaveResult>)method.Invoke(null, [handler, value, patch, fields, ct])!;
    }

    private static Task<ConfigurationSaveResult> SaveTyped<T>(UpdateConfigurationCommandHandler handler, T value, bool patch,
        string[] fields, CancellationToken ct) where T : IBlogSettings<T> =>
        patch ? handler.HandleAsync(PatchConfigurationCommand.Create<T>(
                current => (T)SettingChangeCases.Copy(current), JsonSerializer.SerializeToElement(
                    fields.ToDictionary(key => key, key => typeof(T).GetProperty(key)!.GetValue(value))), fields), ct)
            : handler.HandleAsync(UpdateConfigurationCommand.Create(value), ct);

    [Theory]
    [InlineData("null-object")]
    [InlineData("array-object")]
    [InlineData("empty-object")]
    [InlineData("unknown-field")]
    [InlineData("duplicate-field")]
    [InlineData("null-integer")]
    [InlineData("object-integer")]
    [InlineData("overflow-integer")]
    public void Patch_InvalidShapeOrField_RejectsWithoutExposingSubmittedValue(string scenario)
    {
        var json = scenario switch
        {
            "null-object" => "null",
            "array-object" => "[]",
            "empty-object" => "{}",
            "unknown-field" => """{"B3Unknown":"B3-PROTECTED-SENTINEL"}""",
            "duplicate-field" => """{"FeedItemCount":1,"feedItemCount":2}""",
            "null-integer" => """{"FeedItemCount":null}""",
            "object-integer" => """{"FeedItemCount":{"private":"B3-PROTECTED-SENTINEL"}}""",
            _ => """{"FeedItemCount":2147483648}"""
        };
        var error = Assert.Throws<ValidationException>(() =>
            PatchConfigurationCommand.Create<FeedSettings>(static value => value with { },
                JsonSerializer.Deserialize<JsonElement>(json), nameof(FeedSettings.FeedItemCount)));

        Assert.DoesNotContain("B3-PROTECTED-SENTINEL", error.Message);
        if (scenario is "duplicate-field" or "null-integer" or "object-integer" or "overflow-integer")
            Assert.Equal([nameof(FeedSettings.FeedItemCount)], error.ValidationResult!.MemberNames);
    }
}
