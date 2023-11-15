namespace Moonglade.Core.PageFeature;

public record CreatePageCommand(EditPageRequest Payload) : IRequest<Guid>;

public class CreatePageCommandHandler(IRepository<PageEntity> repo, IMediator mediator) : IRequestHandler<CreatePageCommand, Guid>
{
    public async Task<Guid> Handle(CreatePageCommand request, CancellationToken ct)
    {
        var slug = request.Payload.Slug.ToLower().Trim();

        Guid? cssId = null;
        if (!string.IsNullOrWhiteSpace(request.Payload.CssContent))
        {
            cssId = await mediator.Send(new SaveStyleSheetCommand(Guid.NewGuid(), slug, request.Payload.CssContent), ct);
        }

        var uid = Guid.NewGuid();
        var page = new PageEntity
        {
            Id = uid,
            Title = request.Payload.Title.Trim(),
            Slug = slug,
            MetaDescription = request.Payload.MetaDescription,
            CreateTimeUtc = DateTime.UtcNow,
            HtmlContent = request.Payload.RawHtmlContent,
            HideSidebar = request.Payload.HideSidebar,
            IsPublished = request.Payload.IsPublished,
            CssId = cssId.ToString()
        };

        await repo.AddAsync(page, ct);

        return uid;
    }
}