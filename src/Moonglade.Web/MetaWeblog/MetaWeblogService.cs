using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DateTimeOps;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auth;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.ImageStorage;
using Moonglade.Pages;
using Moonglade.Utils;
using WilderMinds.MetaWeblog;
using Page = WilderMinds.MetaWeblog.Page;
using Post = WilderMinds.MetaWeblog.Post;
using Tag = WilderMinds.MetaWeblog.Tag;

namespace Moonglade.Web.MetaWeblog
{
    public class MetaWeblogService : IMetaWeblogProvider
    {
        private readonly AuthenticationSettings _authenticationSettings;
        private readonly IBlogConfig _blogConfig;
        private readonly IDateTimeResolver _dateTimeResolver;
        private readonly ILogger<MetaWeblogService> _logger;
        private readonly ITagService _tagService;
        private readonly ICategoryService _categoryService;
        private readonly IPostService _postService;
        private readonly IPageService _pageService;
        private readonly IBlogImageStorage _blogImageStorage;
        private readonly IFileNameGenerator _fileNameGenerator;

        public MetaWeblogService(
            IOptions<AuthenticationSettings> authOptions,
            IBlogConfig blogConfig,
            IDateTimeResolver dateTimeResolver,
            ILogger<MetaWeblogService> logger,
            ITagService tagService,
            ICategoryService categoryService,
            IPostService postService,
            IPageService pageService,
            IBlogImageStorage blogImageStorage,
            IFileNameGenerator fileNameGenerator)
        {
            _authenticationSettings = authOptions.Value;
            _blogConfig = blogConfig;
            _dateTimeResolver = dateTimeResolver;
            _logger = logger;
            _tagService = tagService;
            _categoryService = categoryService;
            _postService = postService;
            _pageService = pageService;
            _blogImageStorage = blogImageStorage;
            _fileNameGenerator = fileNameGenerator;
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
                    blogName = _blogConfig.GeneralSettings.SiteTitle,
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
                return ToMetaWeblogPost(post);
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

            try
            {
                await Task.CompletedTask;
                if (numberOfPosts < 0) throw new ArgumentOutOfRangeException(nameof(numberOfPosts));

                // TODO: Get recent posts
                return Array.Empty<Post>();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new MetaWeblogException(e.Message);
            }
        }

        public async Task<string> AddPostAsync(string blogid, string username, string password, Post post, bool publish)
        {
            EnsureUser(username, password);

            try
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

                var p = await _postService.CreateAsync(req);
                return p.Id.ToString();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new MetaWeblogException(e.Message);
            }
        }

        public async Task<bool> DeletePostAsync(string key, string postid, string username, string password, bool publish)
        {
            EnsureUser(username, password);

            try
            {
                if (!Guid.TryParse(postid.Trim(), out var id))
                {
                    throw new ArgumentException("Invalid ID", nameof(postid));
                }

                await _postService.DeleteAsync(id, publish);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new MetaWeblogException(e.Message);
            }
        }

        public async Task<bool> EditPostAsync(string postid, string username, string password, Post post, bool publish)
        {
            EnsureUser(username, password);

            try
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

                await _postService.UpdateAsync(id, req);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new MetaWeblogException(e.Message);
            }
        }

        public async Task<CategoryInfo[]> GetCategoriesAsync(string blogid, string username, string password)
        {
            EnsureUser(username, password);

            try
            {
                var cats = await _categoryService.GetAll();
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

            try
            {
                await _categoryService.CreateAsync(new()
                {
                    DisplayName = category.name.Trim(),
                    Note = category.description.Trim(),
                    RouteName = category.slug.ToLower()
                });

                return 996;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new MetaWeblogException(e.Message);
            }
        }

        public async Task<Tag[]> GetTagsAsync(string blogid, string username, string password)
        {
            EnsureUser(username, password);

            try
            {
                var names = await _tagService.GetAllNames();
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

            try
            {
                // TODO: Check extension names

                var bits = Convert.FromBase64String(mediaObject.bits);

                var pFilename = _fileNameGenerator.GetFileName(mediaObject.name);
                var filename = await _blogImageStorage.InsertAsync(pFilename, bits);

                var imageUrl = $"{Helper.ResolveRootUrl(null, _blogConfig.GeneralSettings.CanonicalPrefix, true)}image/{filename}";

                MediaObjectInfo objectInfo = new MediaObjectInfo { url = imageUrl };
                return objectInfo;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new MetaWeblogException(e.Message);
            }
        }

        public async Task<Page> GetPageAsync(string blogid, string pageid, string username, string password)
        {
            EnsureUser(username, password);

            try
            {
                if (!Guid.TryParse(pageid, out var id))
                {
                    throw new ArgumentException("Invalid ID", nameof(pageid));
                }

                var page = await _pageService.GetAsync(id);
                return ToMetaWeblogPage(page);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new MetaWeblogException(e.Message);
            }
        }

        public async Task<Page[]> GetPagesAsync(string blogid, string username, string password, int numPages)
        {
            EnsureUser(username, password);

            try
            {
                if (numPages < 0) throw new ArgumentOutOfRangeException(nameof(numPages));

                var pages = await _pageService.GetAsync(numPages);
                var mPages = pages.Select(ToMetaWeblogPage);

                return mPages.ToArray();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new MetaWeblogException(e.Message);
            }
        }

        public async Task<Author[]> GetAuthorsAsync(string blogid, string username, string password)
        {
            EnsureUser(username, password);

            try
            {
                await Task.CompletedTask;

                return new[]
                {
                    new Author
                    {
                        display_name = _blogConfig.GeneralSettings.OwnerName
                    }
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new MetaWeblogException(e.Message);
            }
        }

        public async Task<string> AddPageAsync(string blogid, string username, string password, Page page, bool publish)
        {
            EnsureUser(username, password);

            try
            {
                var pageRequest = new UpdatePageRequest
                {
                    Title = page.title,
                    HideSidebar = true,
                    MetaDescription = string.Empty,
                    HtmlContent = page.description,
                    CssContent = string.Empty,
                    IsPublished = publish,
                    Slug = ToSlug(page.title)
                };

                var uid = await _pageService.CreateAsync(pageRequest);
                return uid.ToString();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new MetaWeblogException(e.Message);
            }
        }

        public async Task<bool> EditPageAsync(string blogid, string pageid, string username, string password, Page page, bool publish)
        {
            EnsureUser(username, password);

            try
            {
                if (!Guid.TryParse(pageid, out var id))
                {
                    throw new ArgumentException("Invalid ID", nameof(pageid));
                }

                var pageRequest = new UpdatePageRequest
                {
                    Title = page.title,
                    HideSidebar = true,
                    MetaDescription = string.Empty,
                    HtmlContent = page.description,
                    CssContent = string.Empty,
                    IsPublished = publish,
                    Slug = ToSlug(page.title)
                };

                await _pageService.UpdateAsync(id, pageRequest);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new MetaWeblogException(e.Message);
            }
        }

        public async Task<bool> DeletePageAsync(string blogid, string username, string password, string pageid)
        {
            EnsureUser(username, password);

            try
            {
                if (!Guid.TryParse(pageid, out var id))
                {
                    throw new ArgumentException("Invalid ID", nameof(pageid));
                }

                await _pageService.DeleteAsync(id);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new MetaWeblogException(e.Message);
            }
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

        private Page ToMetaWeblogPage(Pages.Page page)
        {
            var mPage = new Page
            {
                title = page.Title,
                description = page.RawHtmlContent,
                dateCreated = _dateTimeResolver.ToTimeZone(page.CreateTimeUtc),
                categories = Array.Empty<string>(),
                page_id = page.Id.ToString(),
                wp_author_id = _blogConfig.GeneralSettings.OwnerName
            };

            return mPage;
        }

        private async Task<Guid[]> GetCatIds(string[] mPostCategories)
        {
            var allCats = await _categoryService.GetAll();
            var cids = (from postCategory in mPostCategories
                        select allCats.FirstOrDefault(category => category.DisplayName == postCategory)
                        into cat
                        where null != cat
                        select cat.Id).ToArray();

            return cids;
        }
    }
}
