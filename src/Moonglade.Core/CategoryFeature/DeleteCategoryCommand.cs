using Edi.CacheAside.InMemory;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;

namespace Moonglade.Features.CategoryFeature;

public record DeleteCategoryCommand(Guid Id) : ICommand<OperationCode>;

public class DeleteCategoryCommandHandler(
    MoongladeRepository<CategoryEntity> catRepo,
    ICacheAside cache,
    ILogger<DeleteCategoryCommandHandler> logger)
    : ICommandHandler<DeleteCategoryCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(DeleteCategoryCommand request, CancellationToken ct)
    {
        var cat = await catRepo.GetByIdAsync(request.Id, ct);
        if (null == cat) return OperationCode.ObjectNotFound;

        cat.PostCategory.Clear();

        await catRepo.DeleteAsync(cat, ct);
        cache.Remove(BlogCachePartition.General.ToString(), "allcats");

        logger.LogInformation("Category deleted: {Category}", cat.Id);
        return OperationCode.Done;
    }
}