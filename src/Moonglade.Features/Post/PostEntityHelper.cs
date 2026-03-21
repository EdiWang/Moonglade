using Microsoft.Extensions.Logging;
using Moonglade.Data.Specifications;
using Moonglade.Utils;

namespace Moonglade.Features.Post;

internal static class PostEntityHelper
{
    /// <summary>
    /// Resolves tags from a comma-separated string, creating any that don't exist,
    /// and assigns them to the post. Existing tags on the post are cleared first.
    /// </summary>
    public static async Task ResolveAndAssignTagsAsync(
        PostEntity post,
        string tagString,
        IRepositoryBase<TagEntity> tagRepo,
        ILogger logger,
        CancellationToken ct)
    {
        post.Tags.Clear();

        var tags = string.IsNullOrWhiteSpace(tagString)
            ? []
            : tagString.Split(',').ToArray();

        foreach (var item in tags)
        {
            if (!BlogTagHelper.IsValidTagName(item)) continue;

            var tag = await tagRepo.FirstOrDefaultAsync(new TagByDisplayNameSpec(item), ct)
                      ?? await CreateTagAsync(item, tagRepo, logger, ct);

            post.Tags.Add(tag);
        }
    }

    /// <summary>
    /// Sets the categories on a post. Existing categories on the post are cleared first.
    /// </summary>
    public static void SetCategories(PostEntity post, Guid[] categoryIds)
    {
        post.PostCategory.Clear();

        if (categoryIds is not { Length: > 0 }) return;

        foreach (var id in categoryIds)
        {
            post.PostCategory.Add(new()
            {
                PostId = post.Id,
                CategoryId = id
            });
        }
    }

    private static async Task<TagEntity> CreateTagAsync(
        string name,
        IRepositoryBase<TagEntity> tagRepo,
        ILogger logger,
        CancellationToken ct)
    {
        var newTag = new TagEntity
        {
            DisplayName = name,
            NormalizedName = BlogTagHelper.NormalizeName(name, BlogTagHelper.TagNormalizationDictionary)
        };

        var tag = await tagRepo.AddAsync(newTag, ct);
        logger.LogInformation("Created tag: {TagName}", tag.DisplayName);
        return tag;
    }
}
