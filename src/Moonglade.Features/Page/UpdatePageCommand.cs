using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;

namespace Moonglade.Features.Page;

public record UpdatePageCommand(Guid Id, EditPageRequest Payload) : ICommand<Guid>;

public class UpdatePageCommandHandler(
    MoongladeRepository<PageEntity> repo,
    ICommandMediator commandMediator,
    ILogger<UpdatePageCommandHandler> logger) : ICommandHandler<UpdatePageCommand, Guid>
{
    public async Task<Guid> HandleAsync(UpdatePageCommand request, CancellationToken ct)
    {
        var (guid, payload) = request;
        var page = await repo.GetByIdAsync(guid, ct) ?? throw new InvalidOperationException($"PageEntity with Id '{guid}' not found.");
        var slug = request.Payload.Slug.ToLower().Trim();

        Guid? cssId = null;
        if (!string.IsNullOrWhiteSpace(request.Payload.CssContent))
        {
            cssId = await commandMediator.SendAsync(new SaveStyleSheetCommand(page.Id, slug, request.Payload.CssContent), ct);
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