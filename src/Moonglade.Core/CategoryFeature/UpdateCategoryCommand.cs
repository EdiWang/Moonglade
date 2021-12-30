using MediatR;
using Moonglade.Caching;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Core.CategoryFeature;

public class UpdateCategoryCommand : IRequest<OperationCode>
{
    public UpdateCategoryCommand(Guid id, EditCategoryRequest payload)
    {
        Id = id;
        Payload = payload;
    }

    public Guid Id { get; set; }
    public EditCategoryRequest Payload { get; set; }
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

        cat.RouteName = request.Payload.RouteName.Trim();
        cat.DisplayName = request.Payload.DisplayName.Trim();
        cat.Note = request.Payload.Note?.Trim();

        await _catRepo.UpdateAsync(cat);
        _cache.Remove(CacheDivision.General, "allcats");

        return OperationCode.Done;
    }
}