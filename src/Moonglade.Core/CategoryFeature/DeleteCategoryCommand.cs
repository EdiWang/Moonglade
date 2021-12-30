using MediatR;
using Moonglade.Caching;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Core.CategoryFeature;

public class DeleteCategoryCommand : IRequest<OperationCode>
{
    public DeleteCategoryCommand(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; set; }
}

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