using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Core.PageFeature;

public record UpdatePageCommand(Guid Id, EditPageRequest Payload) : IRequest<Guid>;

public class UpdatePageCommandHandler : IRequestHandler<UpdatePageCommand, Guid>
{
    private readonly IRepository<PageEntity> _pageRepo;

    public UpdatePageCommandHandler(IRepository<PageEntity> pageRepo)
    {
        _pageRepo = pageRepo;
    }

    public async Task<Guid> Handle(UpdatePageCommand request, CancellationToken cancellationToken)
    {
        var page = await _pageRepo.GetAsync(request.Id);
        if (page is null)
        {
            throw new InvalidOperationException($"PageEntity with Id '{request.Id}' not found.");
        }

        page.Title = request.Payload.Title.Trim();
        page.Slug = request.Payload.Slug.ToLower().Trim();
        page.MetaDescription = request.Payload.MetaDescription;
        page.HtmlContent = request.Payload.RawHtmlContent;
        page.CssContent = request.Payload.CssContent;
        page.HideSidebar = request.Payload.HideSidebar;
        page.UpdateTimeUtc = DateTime.UtcNow;
        page.IsPublished = request.Payload.IsPublished;

        await _pageRepo.UpdateAsync(page);

        return page.Id;
    }
}