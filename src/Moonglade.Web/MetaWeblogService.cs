using MediatR;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.ImageStorage;
using Moonglade.Utils;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WilderMinds.MetaWeblog;
using Post = WilderMinds.MetaWeblog.Post;
using Tag = WilderMinds.MetaWeblog.Tag;

namespace Moonglade.Web
{
    public class MetaWeblogService : IMetaWeblogProvider
    {
        private readonly IBlogConfig _blogConfig;
        private readonly ITimeZoneResolver _timeZoneResolver;
        private readonly ILogger<MetaWeblogService> _logger;
        private readonly ITagService _tagService;
        private readonly ICategoryService _categoryService;
        private readonly IPostQueryService _postQueryService;
        private readonly IPostManageService _postManageService;
        private readonly IBlogPageService _blogPageService;
        private readonly IBlogImageStorage _blogImageStorage;
        private readonly IFileNameGenerator _fileNameGenerator;
        private readonly IMediator _mediator;

        public MetaWeblogService(
            IBlogConfig blogConfig,
            ITimeZoneResolver timeZoneResolver,
            ILogger<MetaWeblogService> logger,
            ITagService tagService,
            ICategoryService categoryService,
            IPostQueryService postQueryService,
            IPostManageService postManageService,
            IBlogPageService blogPageService,
            IBlogImageStorage blogImageStorage,
            IFileNameGenerator fileNameGenerator,
            IMediator mediator)
        {
            _blogConfig = blogConfig;
            _timeZoneResolver = timeZoneResolver;
            _logger = logger;
            _tagService = tagService;
            _categoryService = categoryService;
            _postQueryService = postQueryService;
            _blogPageService = blogPageService;
            _blogImageStorage = blogImageStorage;
            _fileNameGenerator = fileNameGenerator;
            _mediator = mediator;
            _postManageService = postManageService;
        }

        public Task<UserInfo> GetUserInfoAsync(string key, string username, string password)
        {
            EnsureUser(username, password);

            return TryExecute(() =>
            {
                var user = new UserInfo
                {
                    email = _blogConfig.GeneralSettings.OwnerEmail,
                    firstname = _blogConfig.GeneralSettings.OwnerName,
                    lastname = string.Empty,
                    nickname = string.Empty,
                    url = _blogConfig.GeneralSettings.CanonicalPrefix,
                    userid = key
                };

                return Task.FromResult(user);
            });
        }

        public Task<BlogInfo[]> GetUsersBlogsAsync(string key, string username, string password)
        {
            EnsureUser(username, password);

            return TryExecute(() =>
            {
                var blog = new BlogInfo
                {
                    blogid = _blogConfig.GeneralSettings.SiteTitle,
                    blogName = _blogConfig.GeneralSettings.SiteTitle,
                    url = "/"
                };

                return Task.FromResult(new[] { blog });
            });
        }

        public Task<Post> GetPostAsync(string postid, string username, string password)
        {
            EnsureUser(username, password);

            return TryExecuteAsync(async () =>
            {
                if (!Guid.TryParse(postid.Trim(), out var id))
                {
                    throw new ArgumentException("Invalid ID", nameof(postid));
                }

                var post = await _postQueryService.GetAsync(id);
                return ToMetaWeblogPost(post);
            });
        }

        public async Task<Post[]> GetRecentPostsAsync(string blogid, string username, string password, int numberOfPosts)
        {
            EnsureUser(username, password);
            await Task.CompletedTask;

            return TryExecute(() =>
            {
                if (numberOfPosts < 0) throw new ArgumentOutOfRangeException(nameof(numberOfPosts));

                // TODO: Get recent posts
                return Array.Empty<Post>();
            });
        }

        public Task<string> AddPostAsync(string blogid, string username, string password, Post post, bool publish)
        {
            EnsureUser(username, password);

            return TryExecuteAsync(async () =>
            {
                var cids = await GetCatIds(post.categories);
                if (cids.Length == 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(post.categories));
                }

                var req = new UpdatePostRequest
                {
                    Title = post.title,
                    Slug = post.wp_slug ?? ToSlug(post.title),
                    EditorContent = post.description,
                    Tags = post.mt_keywords?.Split(','),
                    CategoryIds = cids,
                    ContentLanguageCode = "en-us",
                    IsPublished = publish,
                    EnableComment = true,
                    IsFeedIncluded = true,
                    ExposedToSiteMap = true,
                    PublishDate = DateTime.UtcNow
                };

                var p = await _postManageService.CreateAsync(req);
                return p.Id.ToString();
            });
        }

        public Task<bool> DeletePostAsync(string key, string postid, string username, string password, bool publish)
        {
            EnsureUser(username, password);

            return TryExecuteAsync(async () =>
            {
                if (!Guid.TryParse(postid.Trim(), out var id))
                {
                    throw new ArgumentException("Invalid ID", nameof(postid));
                }

                await _postManageService.DeleteAsync(id, publish);
                return true;
            });
        }

        public Task<bool> EditPostAsync(string postid, string username, string password, Post post, bool publish)
        {
            EnsureUser(username, password);

            return TryExecuteAsync(async () =>
            {
                if (!Guid.TryParse(postid.Trim(), out var id))
                {
                    throw new ArgumentException("Invalid ID", nameof(postid));
                }

                var cids = await GetCatIds(post.categories);
                if (cids.Length == 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(post.categories));
                }

                var req = new UpdatePostRequest
                {
                    Title = post.title,
                    Slug = post.wp_slug ?? ToSlug(post.title),
                    EditorContent = post.description,
                    Tags = post.mt_keywords?.Split(','),
                    CategoryIds = cids,
                    ContentLanguageCode = "en-us",
                    IsPublished = publish,
                    EnableComment = true,
                    IsFeedIncluded = true,
                    ExposedToSiteMap = true,
                    PublishDate = DateTime.UtcNow
                };

                await _postManageService.UpdateAsync(id, req);
                return true;
            });
        }

        public Task<CategoryInfo[]> GetCategoriesAsync(string blogid, string username, string password)
        {
            EnsureUser(username, password);

            return TryExecuteAsync(async () =>
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
            });
        }

        public Task<int> AddCategoryAsync(string key, string username, string password, NewCategory category)
        {
            EnsureUser(username, password);

            return TryExecuteAsync(async () =>
            {
                await _categoryService.CreateAsync(category.name.Trim(), category.slug.ToLower(), category.description.Trim());

                return 996;
            });
        }

        public Task<Tag[]> GetTagsAsync(string blogid, string username, string password)
        {
            EnsureUser(username, password);

            return TryExecuteAsync(async () =>
            {
                var names = await _tagService.GetAllNames();
                var tags = names.Select(p => new Tag
                {
                    name = p
                }).ToArray();

                return tags;
            });
        }

        public Task<MediaObjectInfo> NewMediaObjectAsync(string blogid, string username, string password, MediaObject mediaObject)
        {
            EnsureUser(username, password);

            return TryExecuteAsync(async () =>
            {
                // TODO: Check extension names

                var bits = Convert.FromBase64String(mediaObject.bits);

                var pFilename = _fileNameGenerator.GetFileName(mediaObject.name);
                var filename = await _blogImageStorage.InsertAsync(pFilename, bits);

                var imageUrl = $"{Helper.ResolveRootUrl(null, _blogConfig.GeneralSettings.CanonicalPrefix, true)}image/{filename}";

                var objectInfo = new MediaObjectInfo { url = imageUrl };
                return objectInfo;
            });
        }

        public Task<Page> GetPageAsync(string blogid, string pageid, string username, string password)
        {
            EnsureUser(username, password);

            return TryExecuteAsync(async () =>
            {
                if (!Guid.TryParse(pageid, out var id))
                {
                    throw new ArgumentException("Invalid ID", nameof(pageid));
                }

                var page = await _mediator.Send(new GetPageByIdQuery(id));
                return ToMetaWeblogPage(page);
            });
        }

        public Task<WilderMinds.MetaWeblog.Page[]> GetPagesAsync(string blogid, string username, string password, int numPages)
        {
            EnsureUser(username, password);

            return TryExecuteAsync(async () =>
            {
                if (numPages < 0) throw new ArgumentOutOfRangeException(nameof(numPages));

                var pages = await _mediator.Send(new GetPagesQuery(numPages));
                var mPages = pages.Select(ToMetaWeblogPage);

                return mPages.ToArray();
            });
        }

        public async Task<Author[]> GetAuthorsAsync(string blogid, string username, string password)
        {
            EnsureUser(username, password);
            await Task.CompletedTask;

            return TryExecute(() =>
            {
                return new[]
                {
                    new Author
                    {
                        display_name = _blogConfig.GeneralSettings.OwnerName
                    }
                };
            });
        }

        public Task<string> AddPageAsync(string blogid, string username, string password, WilderMinds.MetaWeblog.Page page, bool publish)
        {
            EnsureUser(username, password);

            return TryExecuteAsync(async () =>
            {
                var pageRequest = new PageEditModel
                {
                    Title = page.title,
                    HideSidebar = true,
                    MetaDescription = string.Empty,
                    RawHtmlContent = page.description,
                    CssContent = string.Empty,
                    IsPublished = publish,
                    Slug = ToSlug(page.title)
                };

                var uid = await _blogPageService.CreateAsync(pageRequest);
                return uid.ToString();
            });
        }

        public Task<bool> EditPageAsync(string blogid, string pageid, string username, string password, WilderMinds.MetaWeblog.Page page, bool publish)
        {
            EnsureUser(username, password);

            return TryExecuteAsync(async () =>
            {
                if (!Guid.TryParse(pageid, out var id))
                {
                    throw new ArgumentException("Invalid ID", nameof(pageid));
                }

                var pageRequest = new PageEditModel
                {
                    Title = page.title,
                    HideSidebar = true,
                    MetaDescription = string.Empty,
                    RawHtmlContent = page.description,
                    CssContent = string.Empty,
                    IsPublished = publish,
                    Slug = ToSlug(page.title)
                };

                await _blogPageService.UpdateAsync(id, pageRequest);
                return true;
            });
        }

        public Task<bool> DeletePageAsync(string blogid, string username, string password, string pageid)
        {
            EnsureUser(username, password);

            return TryExecuteAsync(async () =>
            {
                if (!Guid.TryParse(pageid, out var id))
                {
                    throw new ArgumentException("Invalid ID", nameof(pageid));
                }

                await _mediator.Send(new DeletePageCommand(id));
                return true;
            });
        }

        private void EnsureUser(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            var pwdHash = Helper.HashPassword(password.Trim());

            if (string.Compare(username.Trim(), "moonglade", StringComparison.Ordinal) == 0 &&
                string.Compare(pwdHash, _blogConfig.AdvancedSettings.MetaWeblogPasswordHash.Trim(), StringComparison.Ordinal) == 0) return;

            throw new MetaWeblogException("Authentication failed.");
        }

        private string ToSlug(string title)
        {
            var engSlug = title.GenerateSlug();
            if (!string.IsNullOrWhiteSpace(engSlug)) return engSlug;

            // Chinese and other language title
            var bytes = Encoding.Unicode.GetBytes(title);
            var hexArray = bytes.Select(b => $"{b:x2}");
            var hexName = string.Join(string.Empty, hexArray);

            return hexName;
        }

        private Post ToMetaWeblogPost(Core.Post post)
        {
            if (!post.IsPublished) return null;
            var pubDate = post.PubDateUtc.GetValueOrDefault();
            var link = $"/post/{pubDate.Year}/{pubDate.Month}/{pubDate.Day}/{post.Slug.Trim().ToLower()}";

            var mPost = new Post
            {
                postid = post.Id,
                categories = post.Categories.Select(p => p.DisplayName).ToArray(),
                dateCreated = _timeZoneResolver.ToTimeZone(post.CreateTimeUtc),
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

        private WilderMinds.MetaWeblog.Page ToMetaWeblogPage(BlogPage blogPage)
        {
            var mPage = new WilderMinds.MetaWeblog.Page
            {
                title = blogPage.Title,
                description = blogPage.RawHtmlContent,
                dateCreated = _timeZoneResolver.ToTimeZone(blogPage.CreateTimeUtc),
                categories = Array.Empty<string>(),
                page_id = blogPage.Id.ToString(),
                wp_author_id = _blogConfig.GeneralSettings.OwnerName
            };

            return mPage;
        }

        private async Task<Guid[]> GetCatIds(string[] mPostCategories)
        {
            var allCats = await _categoryService.GetAllAsync();
            var cids = (from postCategory in mPostCategories
                        select allCats.FirstOrDefault(category => category.DisplayName == postCategory)
                        into cat
                        where null != cat
                        select cat.Id).ToArray();

            return cids;
        }

        private T TryExecute<T>(Func<T> action)
        {
            try
            {
                return action();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new MetaWeblogException(e.Message);
            }
        }

        private async Task<T> TryExecuteAsync<T>(Func<Task<T>> func)
        {
            try
            {
                return await func();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new MetaWeblogException(e.Message);
            }
        }
    }
}
