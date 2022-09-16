namespace Moonglade.Core.PageFeature;

public record UpdatePageCommand(Guid Id, EditPageRequest Payload) : IRequest<Guid>;

public class UpdatePageCommandHandler : IRequestHandler<UpdatePageCommand, Guid>
{
    private readonly IRepository<PageEntity> _repo;

    public UpdatePageCommandHandler(IRepository<PageEntity> repo) => _repo = repo;

    public async Task<Guid> Handle(UpdatePageCommand request, CancellationToken ct)
    {
        var (guid, payload) = request;
        var page = await _repo.GetAsync(guid, ct);
        if (page is null)
        {
            throw new InvalidOperationException($"PageEntity with Id '{guid}' not found.");
        }

        page.Title = payload.Title.Trim();
        page.Slug = payload.Slug.ToLower().Trim();
        page.MetaDescription = payload.MetaDescription;
        page.HtmlContent = payload.RawHtmlContent;
        page.CssContent = payload.CssContent;
        page.HideSidebar = payload.HideSidebar;
        page.UpdateTimeUtc = DateTime.UtcNow;
        page.IsPublished = payload.IsPublished;

        await _repo.UpdateAsync(page, ct);

        return page.Id;
    }
}