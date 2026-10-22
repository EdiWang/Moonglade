using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Features.Post;

namespace Moonglade.Features.Category;

public record DeleteCategoryCommand(Guid Id) : ICommand<OperationCode>;

public class DeleteCategoryCommandHandler(
    BlogDbContext db,
    ILogger<DeleteCategoryCommandHandler> logger,
    IEventMediator eventMediator)
    : ICommandHandler<DeleteCategoryCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(DeleteCategoryCommand request, CancellationToken ct)
    {
        var cat = await db.Category.FindAsync([request.Id], ct);
        if (cat == null) return OperationCode.ObjectNotFound;

        db.Category.Remove(cat);
        if (await db.SaveChangesAsync(ct) > 0)
        {
            await eventMediator.PublishAsync(new PostChangedEvent(), cancellationToken: CancellationToken.None);
        }

        logger.LogInformation("Category deleted: {Category}", cat.Id);
        return OperationCode.Done;
    }
}