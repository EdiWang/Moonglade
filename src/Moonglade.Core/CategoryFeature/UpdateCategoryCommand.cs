using Edi.CacheAside.InMemory;
using Moonglade.Data;

namespace Moonglade.Core.CategoryFeature;

public class UpdateCategoryCommand : CreateCategoryCommand, IRequest<OperationCode>
{
    public Guid Id { get; set; }
}

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, OperationCode>
{
    private readonly IRepository<CategoryEntity> _repo;
    private readonly ICacheAside _cache;

    public UpdateCategoryCommandHandler(IRepository<CategoryEntity> repo, ICacheAside cache)
    {
        _repo = repo;
        _cache = cache;
    }

    public async Task<OperationCode> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var cat = await _repo.GetAsync(request.Id, ct);
        if (cat is null) return OperationCode.ObjectNotFound;

        cat.RouteName = request.RouteName.Trim();
        cat.DisplayName = request.DisplayName.Trim();
        cat.Note = request.Note?.Trim();

        await _repo.UpdateAsync(cat, ct);
        _cache.Remove(BlogCachePartition.General.ToString(), "allcats");

        return OperationCode.Done;
    }
}