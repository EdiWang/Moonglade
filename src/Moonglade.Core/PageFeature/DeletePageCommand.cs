namespace Moonglade.Core.PageFeature;

public record DeletePageCommand(Guid Id) : IRequest;

public class DeletePageCommandHandler(IRepository<PageEntity> repo, IMediator mediator) : IRequestHandler<DeletePageCommand>
{
    public async Task Handle(DeletePageCommand request, CancellationToken ct)
    {
        var page = await repo.GetAsync(request.Id, ct);
        if (page is null)
        {
            throw new InvalidOperationException($"PageEntity with Id '{request.Id}' not found.");
        }

        if (page.CssId != null)
        {
            await mediator.Send(new DeleteStyleSheetCommand(new(page.CssId)), ct);
        }

        await repo.DeleteAsync(request.Id, ct);
    }
}