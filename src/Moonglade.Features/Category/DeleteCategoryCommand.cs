using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.Category;

public record DeleteCategoryCommand(Guid Id) : ICommand<OperationCode>;

public class DeleteCategoryCommandHandler(
    IRepositoryBase<CategoryEntity> catRepo,
    ILogger<DeleteCategoryCommandHandler> logger)
    : ICommandHandler<DeleteCategoryCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(DeleteCategoryCommand request, CancellationToken ct)
    {
        var cat = await catRepo.GetByIdAsync(request.Id, ct);
        if (null == cat) return OperationCode.ObjectNotFound;

        cat.PostCategory.Clear();

        await catRepo.DeleteAsync(cat, ct);

        logger.LogInformation("Category deleted: {Category}", cat.Id);
        return OperationCode.Done;
    }
}