using Microsoft.Extensions.Logging;
using Moonglade.Data;

namespace Moonglade.Core.PageFeature;

public record UpdatePageCommand(Guid Id, EditPageRequest Payload) : IRequest<Guid>;

public class UpdatePageCommandHandler(
    MoongladeRepository<PageEntity> repo, 
    IMediator mediator, 
    ILogger<UpdatePageCommandHandler> logger) : IRequestHandler<UpdatePageCommand, Guid>
{
    public async Task<Guid> Handle(UpdatePageCommand request, CancellationToken ct)
    {
        var (guid, payload) = request;
        var page = await repo.GetByIdAsync(guid, ct);
        if (page is null)
        {
            throw new InvalidOperationException($"PageEntity with Id '{guid}' not found.");
        }

        var slug = request.Payload.Slug.ToLower().Trim();

        Guid? cssId = null;
        if (!string.IsNullOrWhiteSpace(request.Payload.CssContent))
        {
            cssId = await mediator.Send(new SaveStyleSheetCommand(page.Id, slug, request.Payload.CssContent), ct);
        }

        page.Title = payload.Title.Trim();
        page.Slug = slug;
        page.MetaDescription = payload.MetaDescription;
        page.HtmlContent = payload.RawHtmlContent;
        page.HideSidebar = payload.HideSidebar;
        page.UpdateTimeUtc = DateTime.UtcNow;
        page.IsPublished = payload.IsPublished;
        page.CssId = cssId.ToString();

        await repo.UpdateAsync(page, ct);

        logger.LogInformation("Page updated: {PageId}", page.Id);
        return page.Id;
    }
}