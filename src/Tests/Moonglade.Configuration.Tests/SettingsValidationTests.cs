using LiteBus.Events.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moq;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Nodes;

namespace Moonglade.Configuration.Tests;

public class SettingsValidationTests
{
    public static TheoryData<string, string> InvalidOrdinarySettings => new()
    {
        { nameof(GeneralSettings), """{"siteTitle":null}""" },
        { nameof(ContentSettings), """{"postListPageSize":0}""" },
        { nameof(ContentSettings), """{"defaultEditor":999}""" },
        { nameof(ContentSettings), """{"postTitleAlignment":999}""" },
        { nameof(GeneralSettings), """{"sideBarOption":999}""" },
        { nameof(CommentSettings), """{"commentProvider":999}""" },
        { nameof(CommentSettings), """{"commentOrder":999}""" },
        { nameof(CommentSettings), """{"wordFilterMode":999}""" },
        { nameof(NotificationSettings), "{\"emailDisplayName\":\"" + new string('x', 65) + "\"}" },
        { nameof(FeedSettings), """{"feedItemCount":-1}""" },
        { nameof(ImageSettings), """{"isWatermarkEnabled":false,"watermarkFontSize":0}""" },
        { nameof(AppearanceSettings), """{"enableCustomCss":true,"cssCode":""}""" },
        { nameof(AdvancedSettings), """{"headScripts":"<p>invalid-marker</p>"}""" },
        { nameof(CustomMenuSettings), """{"menus":[null]}""" },
        { nameof(CustomMenuSettings), new CustomMenuSettings { Menus = Enumerable.Range(0, 6).Select(_ => new Menu { Title = "Valid" }).ToImmutableList() }.ToJson() }
    };

    [Theory]
    [MemberData(nameof(InvalidOrdinarySettings))]
    public void Load_SemanticallyInvalidOrdinaryGroup_ReturnsWholeGroupDefaultsForRepair(string name, string patch)
    {
        var property = typeof(BlogSettingsSnapshot).GetProperty(name)!;
        var original = InvalidJson(property.PropertyType, patch);
        var saved = new Dictionary<string, string> { [name] = original };
        var (snapshot, repairs) = BlogConfig.Parse(saved);

        Assert.Equal(DefaultJson(property.PropertyType), repairs[name]);
        Assert.Equal(repairs[name], property.GetValue(snapshot).ToJson());
        Assert.Equal(original, saved[name]);
    }

    [Theory]
    [MemberData(nameof(InvalidOrdinarySettings))]
    public async Task Write_InvalidGroup_ChangesNeitherStoreNorRuntime(string name, string patch)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = new BlogDbContext(new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var property = typeof(BlogSettingsSnapshot).GetProperty(name)!;
        var original = DefaultJson(property.PropertyType);
        var timestamp = DateTime.UtcNow.AddDays(-1);
        db.BlogConfiguration.Add(new BlogConfigurationEntity { CfgKey = name, CfgValue = original, LastModifiedTimeUtc = timestamp });
        await db.SaveChangesAsync(ct);
        var config = new BlogConfig(BlogConfig.Parse(new Dictionary<string, string>()).Snapshot);
        var runtime = property.GetValue(config.Capture());
        var value = InvalidJson(property.PropertyType, patch).FromJson(property.PropertyType);
        var factory = typeof(UpdateConfigurationCommand).GetMethod(nameof(UpdateConfigurationCommand.Create))!.MakeGenericMethod(property.PropertyType);
        var command = (UpdateConfigurationCommand)factory.Invoke(null, [value])!;

        var exception = await Assert.ThrowsAsync<ValidationException>(() => new UpdateConfigurationCommandHandler(
            db, config, Mock.Of<IEventMediator>(), new ConfigurationWriteGate(), NullLogger<UpdateConfigurationCommandHandler>.Instance).HandleAsync(command, ct));

        Assert.DoesNotContain("invalid-marker", exception.Message);
        Assert.Same(runtime, property.GetValue(config.Capture()));
        db.ChangeTracker.Clear();
        var row = await db.BlogConfiguration.SingleAsync(ct);
        Assert.Equal(original, row.CfgValue);
        Assert.Equal(timestamp, row.LastModifiedTimeUtc);
    }



    [Fact]
    public void SupportedSettingValues_AreValid()
    {
        var notification = new NotificationSettings { EmailDisplayName = null };
        Assert.True(Validator.TryValidateObject(notification, new ValidationContext(notification), null, true));
        var appearance = new AppearanceSettings { ThemeId = int.MaxValue };
        Assert.True(Validator.TryValidateObject(appearance, new ValidationContext(appearance), null, true));
        var feed = new FeedSettings { FeedItemCount = 0 };
        Assert.True(Validator.TryValidateObject(feed, new ValidationContext(feed), null, true));
        feed = feed with { FeedItemCount = int.MaxValue };
        Assert.True(Validator.TryValidateObject(feed, new ValidationContext(feed), null, true));
    }

    [Theory]
    [InlineData('a', 10240, true)]
    [InlineData('a', 10241, false)]
    [InlineData('\u754c', 3413, true)]
    [InlineData('\u754c', 3414, false)]
    public void CssLimit_UsesUtf8Bytes(char character, int count, bool valid)
    {
        var settings = new AppearanceSettings { EnableCustomCss = true, CssCode = new string(character, count) };
        Assert.Equal(valid, Validator.TryValidateObject(settings, new ValidationContext(settings), null, true));
    }

    [Theory]
    [InlineData("""{"isEnabled":false,"menus":null}""", true)]
    [InlineData("""{"isEnabled":true,"menus":[]}""", true)]
    [InlineData("""{"menus":[{"title":"Link","subMenus":[]}]}""", true)]
    [InlineData("""{"menus":[{"title":"Link","subMenus":null}]}""", false)]
    [InlineData("""{"menus":[{"title":"Link","subMenus":[null]}]}""", false)]
    [InlineData("""{"menus":[{"title":"Link","subMenus":[{"title":"Sub","url":""}]}]}""", false)]
    [InlineData("""{"menus":[{"title":"","subMenus":[]}]}""", false)]
    public void MenuGraph_ValidatesNestedEntries(string patch, bool valid)
    {
        var settings = InvalidJson(typeof(CustomMenuSettings), patch).FromJson<CustomMenuSettings>();
        Assert.Equal(valid, Validator.TryValidateObject(settings, new ValidationContext(settings), null, true));
    }

    private static string DefaultJson(Type type) => type.GetProperty("DefaultValue", BindingFlags.Public | BindingFlags.Static)!.GetValue(null).ToJson();

    private static string InvalidJson(Type type, string patch)
    {
        var json = JsonNode.Parse(DefaultJson(type))!.AsObject();
        foreach (var property in JsonNode.Parse(patch)!.AsObject()) json[property.Key] = property.Value?.DeepClone();
        return json.ToJsonString();
    }
}
