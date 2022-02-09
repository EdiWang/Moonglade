using Moonglade.Caching;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Core.CategoryFeature;

public class CreateCategoryCommand : IRequest
{
    [Required]
    [Display(Name = "Display Name")]
    [MaxLength(64)]
    public string DisplayName { get; set; }

    [Required]
    [Display(Name = "Route Name")]
    [RegularExpression("(?!-)([a-z0-9-]+)")]
    [MaxLength(64)]
    public string RouteName { get; set; }

    [Required]
    [Display(Name = "Description")]
    [MaxLength(128)]
    public string Note { get; set; }
}

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand>
{
    private readonly IRepository<CategoryEntity> _catRepo;
    private readonly IBlogCache _cache;

    public CreateCategoryCommandHandler(IRepository<CategoryEntity> catRepo, IBlogCache cache)
    {
        _catRepo = catRepo;
        _cache = cache;
    }

    public async Task<Unit> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var exists = _catRepo.Any(c => c.RouteName == request.RouteName);
        if (exists) return Unit.Value;

        var category = new CategoryEntity
        {
            Id = Guid.NewGuid(),
            RouteName = request.RouteName.Trim(),
            Note = request.Note?.Trim(),
            DisplayName = request.DisplayName.Trim()
        };

        await _catRepo.AddAsync(category);
        _cache.Remove(CacheDivision.General, "allcats");

        return Unit.Value;
    }
}