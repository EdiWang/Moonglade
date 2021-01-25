using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moonglade.Auth;
using Moonglade.Configuration.Abstraction;
using WilderMinds.MetaWeblog;

namespace Moonglade.Web.MetaWeblog
{
    public class MoongladeMetaWeblogService : IMetaWeblogProvider
    {
        private readonly AuthenticationSettings _authenticationSettings;

        private readonly IBlogConfig _blogConfig;

        public MoongladeMetaWeblogService(IOptions<AuthenticationSettings> authOptions, IBlogConfig blogConfig)
        {
            _blogConfig = blogConfig;
            _authenticationSettings = authOptions.Value;
        }

        public Task<UserInfo> GetUserInfoAsync(string key, string username, string password)
        {
            EnsureUser(username, password);

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

        public Task<BlogInfo[]> GetUsersBlogsAsync(string key, string username, string password)
        {
            EnsureUser(username, password);

            var blog = new BlogInfo
            {
                blogid = _blogConfig.GeneralSettings.SiteTitle,
                blogName = _blogConfig.GeneralSettings.Description,
                url = "/"
            };

            return Task.FromResult(new[] { blog });
        }

        public async Task<Post> GetPostAsync(string postid, string username, string password)
        {
            EnsureUser(username, password);

            throw new NotImplementedException();
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

            throw new NotImplementedException();
        }

        public async Task<int> AddCategoryAsync(string key, string username, string password, NewCategory category)
        {
            EnsureUser(username, password);

            throw new NotImplementedException();
        }

        public async Task<Tag[]> GetTagsAsync(string blogid, string username, string password)
        {
            EnsureUser(username, password);

            throw new NotImplementedException();
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
            if (string.Compare(username.Trim(), _authenticationSettings.MetaWeblog.Username.Trim(),
                    StringComparison.Ordinal) != 0 ||
                string.Compare(password.Trim(), _authenticationSettings.MetaWeblog.Password.Trim(),
                    StringComparison.Ordinal) != 0) throw new MetaWeblogException("Authentication failed.");
        }
    }
}
