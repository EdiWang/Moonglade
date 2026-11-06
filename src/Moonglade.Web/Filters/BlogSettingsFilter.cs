using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Web.Rendering;

namespace Moonglade.Web.Filters;

public sealed class BlogSettingsFilter(IBlogConfig reader) : IAsyncPageFilter, IResultFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        if (context.HandlerInstance is PageModel model) model.ViewData.CaptureSettings(reader);
        else if (context.HandlerInstance is Page page) page.PageContext.ViewData.CaptureSettings(reader);
        await next();
    }

    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is ViewResult view) view.ViewData.CaptureSettings(reader);
        else if (context.Result is PartialViewResult partial) partial.ViewData.CaptureSettings(reader);
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}
