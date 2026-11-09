using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Localization;
using Moq;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace Moonglade.Web.Tests.Pages.Admin;

public class MenuPageTests
{
    [Fact]
    public async Task ToolbarSave_AssociatesWithMenuForm_WithoutChangingSubmitBinding()
    {
        var item = Assert.Single(new RazorCompiledItemLoader().LoadItems(typeof(Program).Assembly),
            item => item.Identifier == "/Pages/Admin/Menu.cshtml");
        var page = Assert.IsAssignableFrom<RazorPageBase>(Activator.CreateInstance(item.Type));
        var localizer = new Mock<IStringLocalizer<Program>>();
        localizer.Setup(value => value[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.ApplicationName).Returns(typeof(Program).Assembly.GetName().Name!);
        environment.SetupGet(value => value.ContentRootFileProvider).Returns(new NullFileProvider());
        environment.SetupGet(value => value.WebRootFileProvider).Returns(new NullFileProvider());
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(environment.Object);
        services.AddSingleton<DiagnosticListener>(new DiagnosticListener(nameof(MenuPageTests)));
        services.AddSingleton<DiagnosticSource>(provider => provider.GetRequiredService<DiagnosticListener>());
        services.AddControllersWithViews();
        await using var provider = services.BuildServiceProvider();
        item.Type.GetProperty("SharedLocalizer")!.SetValue(page, localizer.Object);
        using var writer = new StringWriter();
        page.HtmlEncoder = HtmlEncoder.Default;
        page.ViewContext = new ViewContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = provider },
            FormContext = new FormContext(),
            ViewData = (ViewDataDictionary)Activator.CreateInstance(
                typeof(ViewDataDictionary<>).MakeGenericType(item.Type),
                new EmptyModelMetadataProvider(), new ModelStateDictionary())!,
            Writer = writer
        };
        var htmlProperty = item.Type.GetProperty("Html")!;
        var html = provider.GetRequiredService(htmlProperty.PropertyType);
        ((IViewContextAware)html).Contextualize(page.ViewContext);
        htmlProperty.SetValue(page, html);

        await page.ExecuteAsync();
        var body = writer.ToString();
        writer.GetStringBuilder().Clear();
        await page.SectionWriters["admintoolbar"]();
        var toolbar = writer.ToString();

        var form = Regex.Match(body, "<form\\b[^>]*\\bid=\"form-settings\"[^>]*>").Value;
        Assert.Contains("x-on:submit.prevent=\"saveSettings()\"", form);
        var submit = Assert.Single(Regex.Matches(toolbar + body, "<button\\b[^>]*>")
            .Cast<System.Text.RegularExpressions.Match>(),
            match => match.Value.Contains("type=\"submit\"", StringComparison.Ordinal));
        Assert.Contains("form=\"form-settings\"", submit.Value);
        Assert.Contains(":disabled=\"isLoading\"", submit.Value);
        Assert.Contains(submit.Value, toolbar);
        Assert.DoesNotContain("x-on:click", submit.Value);
        Assert.Contains("x-on:submit.prevent=\"saveMenuItem()\"", body);
        Assert.Contains("x-on:submit.prevent=\"saveSubMenuItem()\"", body);
    }
}
