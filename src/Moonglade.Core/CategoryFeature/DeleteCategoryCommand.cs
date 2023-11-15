using Edi.CacheAside.InMemory;
using Moonglade.Data;

namespace Moonglade.Core.CategoryFeature;

public record DeleteCategoryCommand(Guid Id) : IRequest<OperationCode>;

public class DeleteCategoryCommandHandler(
        IRepository<CategoryEntity> catRepo,
        IRepository<PostCategoryEntity> postCatRepo,
        ICacheAside cache) : IRequestHandler<DeleteCategoryCommand, OperationCode>
{
    public async Task<OperationCode> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var exists = await catRepo.AnyAsync(c => c.Id == request.Id, ct);
        if (!exists) return OperationCode.ObjectNotFound;

        var pcs = await postCatRepo.GetAsync(pc => pc.CategoryId == request.Id);
        if (pcs is not null) await postCatRepo.DeleteAsync(pcs, ct);

        await catRepo.DeleteAsync(request.Id, ct);
        cache.Remove(BlogCachePartition.General.ToString(), "allcats");

        return OperationCode.Done;
    }
}