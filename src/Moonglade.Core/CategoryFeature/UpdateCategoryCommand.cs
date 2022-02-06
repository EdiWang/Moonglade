using Moonglade.Caching;
using Moonglade.Data;

namespace Moonglade.Core.CategoryFeature;

public class UpdateCategoryCommand : CreateCategoryCommand, IRequest<OperationCode>
{
    public Guid Id { get; set; }
}

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, OperationCode>
{
    private readonly IRepository<CategoryEntity> _catRepo;
    private readonly IBlogCache _cache;

    public UpdateCategoryCommandHandler(IRepository<CategoryEntity> catRepo, IBlogCache cache)
    {
        _catRepo = catRepo;
        _cache = cache;
    }

    public async Task<OperationCode> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var cat = await _catRepo.GetAsync(request.Id);
        if (cat is null) return OperationCode.ObjectNotFound;

        cat.RouteName = request.RouteName.Trim();
        cat.DisplayName = request.DisplayName.Trim();
        cat.Note = request.Note?.Trim();

        await _catRepo.UpdateAsync(cat);
        _cache.Remove(CacheDivision.General, "allcats");

        return OperationCode.Done;
    }
}