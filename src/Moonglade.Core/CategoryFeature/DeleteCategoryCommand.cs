using Edi.CacheAside.InMemory;
using Moonglade.Data;

namespace Moonglade.Core.CategoryFeature;

public record DeleteCategoryCommand(Guid Id) : IRequest<OperationCode>;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, OperationCode>
{
    private readonly IRepository<CategoryEntity> _catRepo;
    private readonly IRepository<PostCategoryEntity> _postCatRepo;
    private readonly ICacheAside _cache;

    public DeleteCategoryCommandHandler(
        IRepository<CategoryEntity> catRepo,
        IRepository<PostCategoryEntity> postCatRepo,
        ICacheAside cache)
    {
        _catRepo = catRepo;
        _postCatRepo = postCatRepo;
        _cache = cache;
    }

    public async Task<OperationCode> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var exists = await _catRepo.AnyAsync(c => c.Id == request.Id, ct);
        if (!exists) return OperationCode.ObjectNotFound;

        var pcs = await _postCatRepo.GetAsync(pc => pc.CategoryId == request.Id);
        if (pcs is not null) await _postCatRepo.DeleteAsync(pcs, ct);

        await _catRepo.DeleteAsync(request.Id, ct);
        _cache.Remove(BlogCachePartition.General.ToString(), "allcats");

        return OperationCode.Done;
    }
}