namespace Moonglade.Core.PageFeature;

public record CreatePageCommand(EditPageRequest Payload) : IRequest<Guid>;

public class CreatePageCommandHandler : IRequestHandler<CreatePageCommand, Guid>
{
    private readonly IRepository<PageEntity> _repo;
    private readonly IMediator _mediator;

    public CreatePageCommandHandler(IRepository<PageEntity> repo, IMediator mediator)
    {
        _repo = repo;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(CreatePageCommand request, CancellationToken ct)
    {
        var slug = request.Payload.Slug.ToLower().Trim();

        Guid? cssId = null;
        if (!string.IsNullOrWhiteSpace(request.Payload.CssContent))
        {
            cssId = await _mediator.Send(new SaveStyleSheetCommand(Guid.NewGuid(), slug, request.Payload.CssContent), ct);
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

        await _repo.AddAsync(page, ct);

        return uid;
    }
}