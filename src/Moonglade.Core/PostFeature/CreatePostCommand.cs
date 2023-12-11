﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Core.TagFeature;
using Moonglade.Data.Generated.Entities;
using Moonglade.Data.Spec;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public record CreatePostCommand(PostEditModel Payload) : IRequest<PostEntity>;

public class CreatePostCommandHandler(IRepository<PostEntity> postRepo,
        ILogger<CreatePostCommandHandler> logger,
        IRepository<TagEntity> tagRepo,
        IConfiguration configuration,
        IBlogConfig blogConfig)
    : IRequestHandler<CreatePostCommand, PostEntity>
{
    public async Task<PostEntity> Handle(CreatePostCommand request, CancellationToken ct)
    {
        var abs = ContentProcessor.GetPostAbstract(
            string.IsNullOrEmpty(request.Payload.Abstract) ? request.Payload.EditorContent : request.Payload.Abstract.Trim(),
            blogConfig.ContentSettings.PostAbstractWords,
            configuration.GetSection("Editor").Get<EditorChoice>() == EditorChoice.Markdown);

        var post = new PostEntity
        {
            CommentEnabled = request.Payload.EnableComment,
            Id = Guid.NewGuid(),
            PostContent = request.Payload.EditorContent,
            ContentAbstract = abs,
            CreateTimeUtc = DateTime.UtcNow,
            LastModifiedUtc = DateTime.UtcNow, // Fix draft orders
            Slug = request.Payload.Slug.ToLower().Trim(),
            Author = request.Payload.Author?.Trim(),
            Title = request.Payload.Title.Trim(),
            ContentLanguageCode = request.Payload.LanguageCode,
            IsFeedIncluded = request.Payload.FeedIncluded,
            PubDateUtc = request.Payload.IsPublished ? DateTime.UtcNow : null,
            IsDeleted = false,
            IsPublished = request.Payload.IsPublished,
            IsFeatured = request.Payload.Featured,
            IsOriginal = string.IsNullOrWhiteSpace(request.Payload.OriginLink),
            OriginLink = string.IsNullOrWhiteSpace(request.Payload.OriginLink) ? null : Helper.SterilizeLink(request.Payload.OriginLink),
            HeroImageUrl = string.IsNullOrWhiteSpace(request.Payload.HeroImageUrl) ? null : Helper.SterilizeLink(request.Payload.HeroImageUrl),
            IsOutdated = request.Payload.IsOutdated
        };

        // check if exist same slug under the same day
        var todayUtc = DateTime.UtcNow.Date;
        if (await postRepo.AnyAsync(new PostSpec(post.Slug, todayUtc), ct))
        {
            var uid = Guid.NewGuid();
            post.Slug += $"-{uid.ToString().ToLower()[..8]}";
            logger.LogInformation($"Found conflict for post slug, generated new slug: {post.Slug}");
        }

        // compute hash
        var input = $"{post.Slug}#{post.PubDateUtc.GetValueOrDefault():yyyyMMdd}";
        var checkSum = Helper.ComputeCheckSum(input);
        post.HashCheckSum = checkSum;

        // add categories
        if (request.Payload.SelectedCatIds is { Length: > 0 })
        {
            foreach (var id in request.Payload.SelectedCatIds)
            {
                post.PostCategory.Add(new()
                {
                    CategoryId = id,
                    PostId = post.Id
                });
            }
        }

        // add tags
        var tags = string.IsNullOrWhiteSpace(request.Payload.Tags) ?
            Array.Empty<string>() :
            request.Payload.Tags.Split(',').ToArray();

        if (tags is { Length: > 0 })
        {
            foreach (var item in tags)
            {
                if (!Tag.ValidateName(item)) continue;

                var tag = await tagRepo.GetAsync(q => q.DisplayName == item) ?? await CreateTag(item);
                post.Tags.Add(tag);
            }
        }

        await postRepo.AddAsync(post, ct);

        return post;
    }

    private async Task<TagEntity> CreateTag(string item)
    {
        var newTag = new TagEntity
        {
            DisplayName = item,
            NormalizedName = Tag.NormalizeName(item, Helper.TagNormalizationDictionary)
        };

        var tag = await tagRepo.AddAsync(newTag);
        return tag;
    }
}