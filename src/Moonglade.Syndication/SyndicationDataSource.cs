﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moonglade.Configuration;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Utils;

namespace Moonglade.Syndication;

public interface ISyndicationDataSource
{
    Task<IReadOnlyList<FeedEntry>> GetFeedDataAsync(string categoryName = null);
}

public class SyndicationDataSource : ISyndicationDataSource
{
    private readonly string _baseUrl;
    private readonly IBlogConfig _blogConfig;
    private readonly IRepository<CategoryEntity> _catRepo;
    private readonly IRepository<PostEntity> _postRepo;
    private readonly IConfiguration _configuration;

    public SyndicationDataSource(
        IBlogConfig blogConfig,
        IHttpContextAccessor httpContextAccessor,
        IRepository<CategoryEntity> catRepo,
        IRepository<PostEntity> postRepo,
        IConfiguration configuration)
    {
        _blogConfig = blogConfig;
        _catRepo = catRepo;
        _postRepo = postRepo;
        _configuration = configuration;

        var acc = httpContextAccessor.HttpContext ?? throw new ArgumentNullException($"{nameof(httpContextAccessor)}.HttpContext is null.");
        _baseUrl = $"{acc.Request.Scheme}://{acc.Request.Host}";
    }

    public async Task<IReadOnlyList<FeedEntry>> GetFeedDataAsync(string categoryName = null)
    {
        IReadOnlyList<FeedEntry> itemCollection;
        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            var cat = await _catRepo.GetAsync(c => c.RouteName == categoryName);
            if (cat is null) return null;

            itemCollection = await GetFeedEntriesAsync(cat.Id);
        }
        else
        {
            itemCollection = await GetFeedEntriesAsync();
        }

        return itemCollection;
    }

    private async Task<IReadOnlyList<FeedEntry>> GetFeedEntriesAsync(Guid? categoryId = null)
    {
        int? top = null;
        if (_blogConfig.FeedSettings.RssItemCount != 0)
        {
            top = _blogConfig.FeedSettings.RssItemCount;
        }

        var postSpec = new PostSpec(categoryId, top);
        var list = await _postRepo.SelectAsync(postSpec, p => p.PubDateUtc != null ? new FeedEntry
        {
            Id = p.Id.ToString(),
            Title = p.Title,
            PubDateUtc = p.PubDateUtc.Value,
            Description = _blogConfig.FeedSettings.UseFullContent ? p.PostContent : p.ContentAbstract,
            Link = $"{_baseUrl}/post/{p.PubDateUtc.Value.Year}/{p.PubDateUtc.Value.Month}/{p.PubDateUtc.Value.Day}/{p.Slug}",
            Author = _blogConfig.FeedSettings.AuthorName,
            AuthorEmail = _blogConfig.GeneralSettings.OwnerEmail,
            Categories = p.PostCategory.Select(pc => pc.Category.DisplayName).ToArray()
        } : null);

        // Workaround EF limitation
        // Man, this is super ugly
        if (_blogConfig.FeedSettings.UseFullContent && list.Any())
        {
            foreach (var simpleFeedItem in list)
            {
                simpleFeedItem.Description = FormatPostContent(simpleFeedItem.Description);
            }
        }

        return list;
    }

    private string FormatPostContent(string rawContent)
    {
        return _configuration.GetSection("Editor").Get<EditorChoice>() == EditorChoice.Markdown ?
            ContentProcessor.MarkdownToContent(rawContent, ContentProcessor.MarkdownConvertType.Html, false) :
            rawContent;
    }
}