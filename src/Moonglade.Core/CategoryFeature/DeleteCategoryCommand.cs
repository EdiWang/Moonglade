using Moonglade.Caching;
using Moonglade.Data;

namespace Moonglade.Core.CategoryFeature;

public record DeleteCategoryCommand(Guid Id) : IRequest<OperationCode>;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, OperationCode>
{
    private readonly IRepository<CategoryEntity> _catRepo;
    private readonly IRepository<PostCategoryEntity> _postCatRepo;
    private readonly IBlogCache _cache;

    public DeleteCategoryCommandHandler(
        IRepository<CategoryEntity> catRepo,
        IRepository<PostCategoryEntity> postCatRepo,
        IBlogCache cache)
    {
        _catRepo = catRepo;
        _postCatRepo = postCatRepo;
        _cache = cache;
    }

    public async Task<OperationCode> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var exists = _catRepo.Any(c => c.Id == request.Id);
        if (!exists) return OperationCode.ObjectNotFound;

        var pcs = await _postCatRepo.GetAsync(pc => pc.CategoryId == request.Id);
        if (pcs is not null) await _postCatRepo.DeleteAsync(pcs);

        await _catRepo.DeleteAsync(request.Id);
        _cache.Remove(CacheDivision.General, "allcats");

        return OperationCode.Done;
    }
}