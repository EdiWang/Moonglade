using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Moonglade.Configuration;
using Moonglade.Web.Filters;
using Moonglade.Web.Rendering;

namespace Moonglade.Web.Tests.Structure;

public class RenderSnapshotBoundaryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResultFilter_PreservesHandlerCaptureForViewAndPartial_WithoutFreezingNextOperation(bool partial)
    {
        var owner = new BlogConfig();
        var data = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        var retained = data.CaptureSettings(owner);
        owner.Publish(retained with { GeneralSettings = retained.GeneralSettings with { SiteTitle = "Updated" } });
        IActionResult result = partial ? new PartialViewResult { ViewData = data } : new ViewResult { ViewData = data };
        var http = new DefaultHttpContext();
        var context = new ResultExecutingContext(new ActionContext(http, new RouteData(), new ActionDescriptor()), [], result, new object());

        new BlogSettingsFilter(owner).OnResultExecuting(context);

        Assert.Same(retained, data.CaptureSettings(owner));
        Assert.Same(retained, new ViewDataDictionary(data).CaptureSettings(owner));
        Assert.NotSame(retained, owner.Capture());
        Assert.Equal("Updated", owner.Capture().GeneralSettings.SiteTitle);
        Assert.Empty(http.Items);
        var nextRender = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        Assert.Same(owner.Capture(), nextRender.CaptureSettings(owner));
    }
}
