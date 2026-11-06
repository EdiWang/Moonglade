using Moonglade.Configuration;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Moonglade.Web.Tests.Structure;

public class ImmutableSettingsCompatibilityTests
{
    [Theory]
    [InlineData("{}")]
    [InlineData("""{"feedItemCount":0,"useFullContent":false}""")]
    [InlineData("""{"FEEDItemCount":1,"UseFullContent":true,"future":"untouched"}""")]
    [InlineData("""{"feedItemCount":2,"feedItemCount":3}""")]
    public void PersistedFeed_ImmutableCandidateMatchesExistingDefaultMerge(string json)
    {
        var config = new BlogConfig();
        Assert.DoesNotContain(nameof(FeedSettings), config.LoadFromConfig(new Dictionary<string, string> { [nameof(FeedSettings)] = json }).Keys);
        var defaults = FeedSettings.DefaultValue.ToJson().FromJson<ImmutableFeedProbe>();

        var candidate = LoadWithDefaults(json, defaults);

        Assert.Equal(config.FeedSettings.ToJson(), candidate.ToJson());
        Assert.Equal(candidate.ToJson(), candidate.ToJson().FromJson<ImmutableFeedProbe>().ToJson());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"isEnabled":false,"menus":null}""")]
    [InlineData("""{"isEnabled":true,"menus":[]}""")]
    [InlineData("""{"isEnabled":true,"menus":[{"title":"Parent","url":null,"subMenus":[{"title":"Child","url":"/child"}]}]}""")]
    public void PersistedMenus_ImmutableGraphPreservesLegacyJsonAndExplicitValues(string json)
    {
        var config = new BlogConfig();
        Assert.DoesNotContain(nameof(CustomMenuSettings), config.LoadFromConfig(new Dictionary<string, string> { [nameof(CustomMenuSettings)] = json }).Keys);
        var defaults = CustomMenuSettings.DefaultValue.ToJson().FromJson<ImmutableMenusProbe>();

        var candidate = LoadWithDefaults(json, defaults);

        Assert.True(Valid(candidate));
        Assert.Equal(config.CustomMenuSettings.ToJson(), candidate.ToJson());
        Assert.Equal(candidate.ToJson(), candidate.ToJson().FromJson<ImmutableMenusProbe>().ToJson());
    }

    [Theory]
    [InlineData("""{"isEnabled":true,"menus":null}""")]
    [InlineData("""{"menus":[null]}""")]
    [InlineData("""{"menus":[{"title":"","subMenus":[]}]}""")]
    [InlineData("""{"menus":[{"title":"Parent","subMenus":null}]}""")]
    [InlineData("""{"menus":[{"title":"Parent","subMenus":[null]}]}""")]
    [InlineData("""{"menus":[{"title":"Parent","subMenus":[{"title":"Child","url":""}]}]}""")]
    public void InvalidMenus_CandidateAndCurrentRulesRejectWithoutMutation(string json)
    {
        var candidate = json.FromJson<ImmutableMenusProbe>();
        var current = json.FromJson<CustomMenuSettings>();
        var before = candidate.ToJson();

        Assert.False(Valid(current));
        Assert.False(Valid(candidate));
        Assert.Equal(before, candidate.ToJson());
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void ImmutableList_ExistingMaxLengthRuleWorks(int count, bool expected)
    {
        var candidate = new ImmutableMenusProbe { Menus = Enumerable.Range(0, count).Select(_ => (ImmutableMenuProbe?)new ImmutableMenuProbe { Title = "Link" }).ToImmutableList() };
        Assert.Equal(expected, Valid(candidate));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void ImmutableArray_MaxLengthCompatibilityProbe(int count, bool expected)
    {
        Assert.Equal(expected, Valid(new ArrayLengthProbe { Values = Enumerable.Range(0, count).ToImmutableArray() }));
    }

    [Fact]
    public void RetainedSnapshot_CollectionChangesProduceNewValues()
    {
        var first = new SettingsSnapshotProbe(new ImmutableFeedProbe { FeedItemCount = 20 },
            new ImmutableMenusProbe { Menus = [new ImmutableMenuProbe { Title = "Parent", SubMenus = [new ImmutableSubMenuProbe { Title = "Before", Url = "/before" }] }] });
        var firstMenu = first.Menu.Menus![0]!;
        var revisedMenu = firstMenu with { SubMenus = firstMenu.SubMenus!.SetItem(0, new ImmutableSubMenuProbe { Title = "After", Url = "/after" }) };
        var second = first with { Menu = first.Menu with { Menus = first.Menu.Menus.SetItem(0, revisedMenu) } };

        Assert.Equal("Before", first.Menu.Menus[0]!.SubMenus![0]!.Title);
        Assert.Equal("After", second.Menu.Menus![0]!.SubMenus![0]!.Title);
        Assert.Throws<NotSupportedException>(() => ((IList<ImmutableMenuProbe?>)first.Menu.Menus).Add(revisedMenu));
    }

    private static bool Valid(object value) => Validator.TryValidateObject(value, new ValidationContext(value), null, true);

    private static T LoadWithDefaults<T>(string json, T defaults) => (T)typeof(JsonExtensions)
        .GetMethod("FromJsonWithDefaults", BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(typeof(T))
        .Invoke(null, [json, defaults])!;

    private sealed record ArrayLengthProbe
    {
        [MaxLength(1)]
        public ImmutableArray<int> Values { get; init; }
    }
}
