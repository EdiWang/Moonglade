using Edi.CacheAside.InMemory;
using Microsoft.Extensions.Logging;
using Moonglade.Data;

namespace Moonglade.Core.CategoryFeature;

public class UpdateCategoryCommand : CreateCategoryCommand, IRequest<OperationCode>
{
    public Guid Id { get; set; }
}

public class UpdateCategoryCommandHandler(
    MoongladeRepository<CategoryEntity> repo,
    ICacheAside cache,
    ILogger<UpdateCategoryCommandHandler> logger) : IRequestHandler<UpdateCategoryCommand, OperationCode>
{
    public async Task<OperationCode> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var cat = await repo.GetByIdAsync(request.Id, ct);
        if (cat is null) return OperationCode.ObjectNotFound;

        cat.Slug = request.Slug.Trim();
        cat.DisplayName = request.DisplayName.Trim();
        cat.Note = request.Note?.Trim();

        await repo.UpdateAsync(cat, ct);
        cache.Remove(BlogCachePartition.General.ToString(), "allcats");

        logger.LogInformation("Category updated: {Category}", cat.Id);
        return OperationCode.Done;
    }
}