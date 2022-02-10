namespace Moonglade.Core.PageFeature;

public record CreatePageCommand(EditPageRequest Payload) : IRequest<Guid>;

public class CreatePageCommandHandler : IRequestHandler<CreatePageCommand, Guid>
{
    private readonly IRepository<PageEntity> _pageRepo;

    public CreatePageCommandHandler(IRepository<PageEntity> pageRepo)
    {
        _pageRepo = pageRepo;
    }

    public async Task<Guid> Handle(CreatePageCommand request, CancellationToken cancellationToken)
    {
        var uid = Guid.NewGuid();
        var page = new PageEntity
        {
            Id = uid,
            Title = request.Payload.Title.Trim(),
            Slug = request.Payload.Slug.ToLower().Trim(),
            MetaDescription = request.Payload.MetaDescription,
            CreateTimeUtc = DateTime.UtcNow,
            HtmlContent = request.Payload.RawHtmlContent,
            CssContent = request.Payload.CssContent,
            HideSidebar = request.Payload.HideSidebar,
            IsPublished = request.Payload.IsPublished
        };

        await _pageRepo.AddAsync(page);

        return uid;
    }
}