using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Features.Post;

namespace Moonglade.Features.Category;

public class UpdateCategoryCommand : CreateCategoryCommand, ICommand<OperationCode>
{
    public Guid Id { get; set; }
}

public class UpdateCategoryCommandHandler(
    BlogDbContext db,
    ILogger<UpdateCategoryCommandHandler> logger,
    IEventMediator eventMediator) : ICommandHandler<UpdateCategoryCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(UpdateCategoryCommand request, CancellationToken ct)
    {
        var cat = await db.Category.FindAsync([request.Id], ct);
        if (cat is null) return OperationCode.ObjectNotFound;

        cat.Slug = request.Slug.Trim();
        cat.DisplayName = request.DisplayName.Trim();
        cat.Note = request.Note?.Trim();

        if (await db.SaveChangesAsync(ct) > 0)
        {
            await eventMediator.PublishAsync(new PostChangedEvent(), cancellationToken: CancellationToken.None);
        }

        logger.LogInformation("Category updated: {Category}", cat.Id);
        return OperationCode.Done;
    }
}