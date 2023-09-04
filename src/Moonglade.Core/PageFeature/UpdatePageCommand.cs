namespace Moonglade.Core.PageFeature;

public record UpdatePageCommand(Guid Id, EditPageRequest Payload) : IRequest<Guid>;

public class UpdatePageCommandHandler : IRequestHandler<UpdatePageCommand, Guid>
{
    private readonly IRepository<PageEntity> _repo;
    private readonly IMediator _mediator;

    public UpdatePageCommandHandler(IRepository<PageEntity> repo, IMediator mediator)
    {
        _repo = repo;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(UpdatePageCommand request, CancellationToken ct)
    {
        var (guid, payload) = request;
        var page = await _repo.GetAsync(guid, ct);
        if (page is null)
        {
            throw new InvalidOperationException($"PageEntity with Id '{guid}' not found.");
        }

        var slug = request.Payload.Slug.ToLower().Trim();

        Guid? cssId = null;
        if (!string.IsNullOrWhiteSpace(request.Payload.CssContent))
        {
            cssId = await _mediator.Send(new SaveStyleSheetCommand(page.Id, slug, request.Payload.CssContent), ct);
        }

        page.Title = payload.Title.Trim();
        page.Slug = slug;
        page.MetaDescription = payload.MetaDescription;
        page.HtmlContent = payload.RawHtmlContent;
        page.HideSidebar = payload.HideSidebar;
        page.UpdateTimeUtc = DateTime.UtcNow;
        page.IsPublished = payload.IsPublished;
        page.CssId = cssId.ToString();

        await _repo.UpdateAsync(page, ct);

        return page.Id;
    }
}