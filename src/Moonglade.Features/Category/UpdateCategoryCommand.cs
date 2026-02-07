using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;

namespace Moonglade.Features.Category;

public class UpdateCategoryCommand : CreateCategoryCommand, ICommand<OperationCode>
{
    public Guid Id { get; set; }
}

public class UpdateCategoryCommandHandler(
    IRepositoryBase<CategoryEntity> repo,
    ILogger<UpdateCategoryCommandHandler> logger) : ICommandHandler<UpdateCategoryCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(UpdateCategoryCommand request, CancellationToken ct)
    {
        var cat = await repo.GetByIdAsync(request.Id, ct);
        if (cat is null) return OperationCode.ObjectNotFound;

        cat.Slug = request.Slug.Trim();
        cat.DisplayName = request.DisplayName.Trim();
        cat.Note = request.Note?.Trim();

        await repo.UpdateAsync(cat, ct);

        logger.LogInformation("Category updated: {Category}", cat.Id);
        return OperationCode.Done;
    }
}