using Edi.CacheAside.InMemory;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Specifications;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Features.CategoryFeature;

public class CreateCategoryCommand : ICommand
{
    [Required]
    [Display(Name = "Display name")]
    [MaxLength(64)]
    public string DisplayName { get; set; }

    [Required]
    [Display(Name = "Slug")]
    [RegularExpression("(?!-)([a-z0-9-]+)")]
    [MaxLength(64)]
    public string Slug { get; set; }

    [Required]
    [Display(Name = "Description")]
    [MaxLength(128)]
    public string Note { get; set; }
}

public class CreateCategoryCommandHandler(
    MoongladeRepository<CategoryEntity> catRepo,
    ICacheAside cache,
    ILogger<CreateCategoryCommandHandler> logger) : ICommandHandler<CreateCategoryCommand>
{
    public async Task HandleAsync(CreateCategoryCommand request, CancellationToken ct)
    {
        var exists = await catRepo.AnyAsync(new CategoryBySlugSpec(request.Slug), ct);
        if (exists) return;

        var category = new CategoryEntity
        {
            Id = Guid.NewGuid(),
            Slug = request.Slug.Trim(),
            Note = request.Note?.Trim(),
            DisplayName = request.DisplayName.Trim()
        };

        await catRepo.AddAsync(category, ct);
        cache.Remove(BlogCachePartition.General.ToString(), "allcats");

        logger.LogInformation("Category created: {Category}", category.Id);
    }
}