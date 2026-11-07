using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace Moonglade.Configuration.Tests;

public class BlogSettingsSnapshotTests
{
    [Fact]
    public void ConsumerBoundary_HasNoLoadOrPublicationAccess_AndSharesItsOwner()
    {
        var services = new ServiceCollection();
        services.AddBlogConfig();
        using var provider = services.BuildServiceProvider();
        var reader = provider.GetRequiredService<IBlogConfig>();
        var owner = provider.GetRequiredService<BlogConfig>();

        Assert.Same(owner, reader);
        Assert.Same(owner.Capture(), reader.Capture());
        Assert.Empty(typeof(IBlogConfig).GetProperties());
        Assert.DoesNotContain(typeof(IBlogConfig).GetMethods(), method => method.Name is "Publish" or "Parse");
        Assert.Equal(9, typeof(BlogSettingsSnapshot).GetProperties().Length);
        Assert.DoesNotContain(typeof(BlogSettingsSnapshot).GetProperties(), property => property.Name is "LocalAccountSettings" or "SystemManifestSettings");
    }

    [Fact]
    public void PublishedValuesAndMenuGraph_ExposeOnlyInitProperties()
    {
        var models = typeof(BlogSettingsSnapshot).GetProperties().Select(property => property.PropertyType)
            .Concat([typeof(BlogSettingsSnapshot), typeof(Menu), typeof(SubMenu)]);

        foreach (var model in models)
        {
            Assert.True(model.IsSealed);
            foreach (var property in model.GetProperties().Where(property => property.GetMethod?.IsStatic == false))
                Assert.Contains(typeof(IsExternalInit), property.SetMethod!.ReturnParameter.GetRequiredCustomModifiers());
        }
    }

    [Fact]
    public void MenuEdits_ProduceNewListsAndValuesWithoutChangingRetainedSnapshots()
    {
        var owner = new BlogConfig(BlogConfig.Parse(new Dictionary<string, string>()).Snapshot);
        var retained = owner.Capture();
        var menu = retained.CustomMenuSettings.Menus[0];
        var edited = menu with
        {
            Title = "Edited",
            SubMenus = menu.SubMenus.Add(new SubMenu { Title = "Child", Url = "/child" })
        };
        var replacement = retained with
        {
            CustomMenuSettings = retained.CustomMenuSettings with
            {
                Menus = retained.CustomMenuSettings.Menus.SetItem(0, edited)
            }
        };

        owner.Publish(replacement);

        Assert.Equal("About", retained.CustomMenuSettings.Menus[0].Title);
        Assert.Empty(retained.CustomMenuSettings.Menus[0].SubMenus);
        Assert.Equal("Edited", owner.Capture().CustomMenuSettings.Menus[0].Title);
        Assert.Equal("/child", Assert.Single(owner.Capture().CustomMenuSettings.Menus[0].SubMenus).Url);
        Assert.Throws<NotSupportedException>(() => ((IList<Menu>)replacement.CustomMenuSettings.Menus).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<SubMenu>)edited.SubMenus).Clear());
    }
}
