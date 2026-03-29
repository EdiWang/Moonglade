using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.Category;

public record DeleteCategoryCommand(Guid Id) : ICommand<OperationCode>;

public class DeleteCategoryCommandHandler(
    BlogDbContext db,
    ILogger<DeleteCategoryCommandHandler> logger)
    : ICommandHandler<DeleteCategoryCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(DeleteCategoryCommand request, CancellationToken ct)
    {
        var cat = await db.Category.FindAsync([request.Id], ct);
        if (null == cat) return OperationCode.ObjectNotFound;

        db.Category.Remove(cat);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Category deleted: {Category}", cat.Id);
        return OperationCode.Done;
    }
}