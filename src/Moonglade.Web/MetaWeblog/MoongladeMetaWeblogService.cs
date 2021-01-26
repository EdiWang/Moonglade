﻿using System;
using System.Linq;
using System.Threading.Tasks;
using DateTimeOps;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auth;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Utils;
using WilderMinds.MetaWeblog;
using Post = WilderMinds.MetaWeblog.Post;
using Tag = WilderMinds.MetaWeblog.Tag;

namespace Moonglade.Web.MetaWeblog
{
    public class MoongladeMetaWeblogService : IMetaWeblogProvider
    {
        private readonly AuthenticationSettings _authenticationSettings;
        private readonly IBlogConfig _blogConfig;
        private readonly IDateTimeResolver _dateTimeResolver;
        private readonly ILogger<MoongladeMetaWeblogService> _logger;
        private readonly ITagService _tagService;
        private readonly ICategoryService _categoryService;
        private readonly IPostService _postService;

        public MoongladeMetaWeblogService(
            IOptions<AuthenticationSettings> authOptions,
            IBlogConfig blogConfig,
            IDateTimeResolver dateTimeResolver,
            ILogger<MoongladeMetaWeblogService> logger,
            ITagService tagService,
            ICategoryService categoryService,
            IPostService postService)
        {
            _authenticationSettings = authOptions.Value;
            _blogConfig = blogConfig;
            _dateTimeResolver = dateTimeResolver;
            _logger = logger;
            _tagService = tagService;
            _categoryService = categoryService;
            _postService = postService;
        }

        public Task<UserInfo> GetUserInfoAsync(string key, string username, string password)
        {
            EnsureUser(username, password);

            try
            {
                var user = new UserInfo
                {
                    email = _blogConfig.NotificationSettings.AdminEmail,
                    firstname = _blogConfig.GeneralSettings.OwnerName,
                    lastname = string.Empty,
                    nickname = string.Empty,
                    url = _blogConfig.GeneralSettings.CanonicalPrefix,
                    userid = key
                };

                return Task.FromResult(user);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new MetaWeblogException(e.Message);
            }
        }

        public Task<BlogInfo[]> GetUsersBlogsAsync(string key, string username, string password)
        {
            EnsureUser(username, password);

            try
            {
                var blog = new BlogInfo
                {
                    blogid = _blogConfig.GeneralSettings.SiteTitle,
                    blogName = _blogConfig.GeneralSettings.Description,
                    url = "/"
                };

                return Task.FromResult(new[] { blog });
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new MetaWeblogException(e.Message);
            }
        }

        public async Task<Post> GetPostAsync(string postid, string username, string password)
        {
            EnsureUser(username, password);

            try
            {
                if (!Guid.TryParse(postid.Trim(), out var id))
                {
                    throw new ArgumentException("Invalid ID", nameof(postid));
                }

                var post = await _postService.GetAsync(id);
                if (!post.IsPublished) return null;
                var pubDate = post.PubDateUtc.GetValueOrDefault();
                var link = $"/post/{pubDate.Year}/{pubDate.Month}/{pubDate.Day}/{post.Slug.Trim().ToLower()}";

                var mPost = new Post
                {
                    postid = id,
                    categories = post.Categories.Select(p => p.DisplayName).ToArray(),
                    dateCreated = _dateTimeResolver.ToTimeZone(post.CreateTimeUtc),
                    description = post.ContentAbstract,
                    link = link,
                    permalink = $"{Helper.ResolveRootUrl(null, _blogConfig.GeneralSettings.CanonicalPrefix, true)}/{link}",
                    title = post.Title,
                    wp_slug = post.Slug,
                    mt_keywords = string.Join(',', post.Tags.Select(p => p.DisplayName)),
                    mt_excerpt = post.ContentAbstract,
                    userid = _blogConfig.GeneralSettings.OwnerName
                };

                return mPost;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new MetaWeblogException(e.Message);
            }
        }

        public async Task<Post[]> GetRecentPostsAsync(string blogid, string username, string password, int numberOfPosts)
        {
            EnsureUser(username, password);

            throw new NotImplementedException();
        }

        public async Task<string> AddPostAsync(string blogid, string username, string password, Post post, bool publish)
        {
            EnsureUser(username, password);

            throw new NotImplementedException();
        }

        public async Task<bool> DeletePostAsync(string key, string postid, string username, string password, bool publish)
        {
            EnsureUser(username, password);

            throw new NotImplementedException();
        }

        public async Task<bool> EditPostAsync(string postid, string username, string password, Post post, bool publish)
        {
            EnsureUser(username, password);

            throw new NotImplementedException();
        }

        public async Task<CategoryInfo[]> GetCategoriesAsync(string blogid, string username, string password)
        {
            EnsureUser(username, password);

            try
            {
                var cats = await _categoryService.GetAllAsync();
                var catInfos = cats.Select(p => new CategoryInfo
                {
                    title = p.DisplayName,
                    categoryid = p.Id.ToString(),
                    description = p.Note,
                    htmlUrl = $"/category/{p.RouteName}",
                    rssUrl = $"/rss/{p.RouteName}"
                }).ToArray();

                return catInfos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new MetaWeblogException(e.Message);
            }
        }

        public async Task<int> AddCategoryAsync(string key, string username, string password, NewCategory category)
        {
            EnsureUser(username, password);

            throw new NotImplementedException();
        }

        public async Task<Tag[]> GetTagsAsync(string blogid, string username, string password)
        {
            EnsureUser(username, password);

            try
            {
                var names = await _tagService.GetAllNamesAsync();
                var tags = names.Select(p => new Tag
                {
                    name = p
                }).ToArray();

                return tags;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new MetaWeblogException(e.Message);
            }
        }

        public async Task<MediaObjectInfo> NewMediaObjectAsync(string blogid, string username, string password, MediaObject mediaObject)
        {
            EnsureUser(username, password);

            throw new NotImplementedException();
        }

        public async Task<Page> GetPageAsync(string blogid, string pageid, string username, string password)
        {
            EnsureUser(username, password);

            throw new NotImplementedException();
        }

        public async Task<Page[]> GetPagesAsync(string blogid, string username, string password, int numPages)
        {
            EnsureUser(username, password);

            throw new NotImplementedException();
        }

        public async Task<Author[]> GetAuthorsAsync(string blogid, string username, string password)
        {
            EnsureUser(username, password);

            throw new NotImplementedException();
        }

        public async Task<string> AddPageAsync(string blogid, string username, string password, Page page, bool publish)
        {
            EnsureUser(username, password);

            throw new NotImplementedException();
        }

        public async Task<bool> EditPageAsync(string blogid, string pageid, string username, string password, Page page, bool publish)
        {
            EnsureUser(username, password);

            throw new NotImplementedException();
        }

        public async Task<bool> DeletePageAsync(string blogid, string username, string password, string pageid)
        {
            EnsureUser(username, password);

            throw new NotImplementedException();
        }

        private void EnsureUser(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            if (string.Compare(username.Trim(), _authenticationSettings.MetaWeblog.Username.Trim(),
                StringComparison.Ordinal) == 0 && string.Compare(password.Trim(),
                _authenticationSettings.MetaWeblog.Password.Trim(),
                StringComparison.Ordinal) == 0) return;

            throw new MetaWeblogException("Authentication failed.");
        }
    }
}
