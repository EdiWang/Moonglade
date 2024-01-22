using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace WilderMinds.MetaWeblog
{
  public class MetaWeblogService : XmlRpcService
  {
    private IMetaWeblogProvider _provider;
    private ILogger<MetaWeblogService> _logger;

    public MetaWeblogService(IMetaWeblogProvider provider, ILogger<MetaWeblogService> logger) : base(logger)
    {
      _provider = provider;
      _logger = logger;
    }

    [XmlRpcMethod("blogger.getUsersBlogs")]
    public async Task<BlogInfo[]> GetUsersBlogsAsync(string key, string username, string password)
    {
      _logger.LogInformation($"MetaWeblog:GetUserBlogs is called");
      return await _provider.GetUsersBlogsAsync(key, username, password);
    }

    [XmlRpcMethod("blogger.getUserInfo")]
    public async Task<UserInfo> GetUserInfoAsync(string key, string username, string password)
    {
      _logger.LogInformation($"MetaWeblog:GetUserInfo is called");
      return await _provider.GetUserInfoAsync(key, username, password);
    }

    [XmlRpcMethod("wp.newCategory")]
    public async Task<int> AddCategoryAsync(string key, string username, string password, NewCategory category)
    {
      _logger.LogInformation($"MetaWeblog:AddCategory is called");
      return await _provider.AddCategoryAsync(key, username, password, category);
    }

    [XmlRpcMethod("metaWeblog.getPost")]
    public async Task<Post> GetPostAsync(string postid, string username, string password)
    {
      _logger.LogInformation($"MetaWeblog:GetPost is called");
      return await _provider.GetPostAsync(postid, username, password);
    }

    [XmlRpcMethod("metaWeblog.getRecentPosts")]
    public async Task<Post[]> GetRecentPostsAsync(string blogid, string username, string password, int numberOfPosts)
    {
      _logger.LogInformation($"MetaWeblog:GetRecentPosts is called");
      return await _provider.GetRecentPostsAsync(blogid, username, password, numberOfPosts);
    }

    [XmlRpcMethod("metaWeblog.newPost")]
    public async Task<string> AddPostAsync(string blogid, string username, string password, Post post, bool publish)
    {
      _logger.LogInformation($"MetaWeblog:AddPost is called");
      return await _provider.AddPostAsync(blogid, username, password, post, publish);
    }

    [XmlRpcMethod("metaWeblog.editPost")]
    public async Task<bool> EditPostAsync(string postid, string username, string password, Post post, bool publish)
    {
      _logger.LogInformation($"MetaWeblog:EditPost is called");
      return await _provider.EditPostAsync(postid, username, password, post, publish);
    }

    [XmlRpcMethod("blogger.deletePost")]
    public async Task<bool> DeletePostAsync(string key, string postid, string username, string password, bool publish)
    {
      _logger.LogInformation($"MetaWeblog:DeletePost is called");
      return await _provider.DeletePostAsync(key, postid, username, password, publish);
    }

    [XmlRpcMethod("metaWeblog.getCategories")]
    public async Task<CategoryInfo[]> GetCategoriesAsync(string blogid, string username, string password)
    {
      _logger.LogInformation($"MetaWeblog:GetCategories is called");
      return await _provider.GetCategoriesAsync(blogid, username, password);
    }

    [XmlRpcMethod("wp.getTags")]
    public async Task<Tag[]> GetTagsAsync(string blogid, string username, string password)
    {
      _logger.LogInformation($"MetaWeblog:GetTagsAsync is called");
      return await _provider.GetTagsAsync(blogid, username, password);
    }

    [XmlRpcMethod("metaWeblog.newMediaObject")]
    public async Task<MediaObjectInfo> NewMediaObjectAsync(string blogid, string username, string password, MediaObject mediaObject)
    {
      _logger.LogInformation($"MetaWeblog:NewMediaObject is called");
      return await _provider.NewMediaObjectAsync(blogid, username, password, mediaObject);
    }

    [XmlRpcMethod("wp.getPage")]
    public async Task<Page> GetPageAsync(string blogid, string pageid, string username, string password)
    {
      _logger.LogInformation($"wp.getPage is called");
      return await _provider.GetPageAsync(blogid, pageid, username, password);
    }

    [XmlRpcMethod("wp.getPages")]
    public async Task<Page[]> GetPagesAsync(string blogid, string username, string password, int numPages)
    {
      _logger.LogInformation($"wp.getPages is called");
      return await _provider.GetPagesAsync(blogid, username, password, numPages);
    }

    [XmlRpcMethod("wp.getAuthors")]
    public async Task<Author[]> GetAuthorsAsync(string blogid, string username, string password)
    {
      _logger.LogInformation($"wp.getAuthors is called");
      return await _provider.GetAuthorsAsync(blogid, username, password);
    }

    [XmlRpcMethod("wp.newPage")]
    public async Task<string> AddPageAsync(string blogid, string username, string password, Page page, bool publish)
    {
      _logger.LogInformation($"wp.newPage is called");
      return await _provider.AddPageAsync(blogid, username, password, page, publish);
    }

    [XmlRpcMethod("wp.editPage")]
    public async Task<bool> EditPageAsync(string blogid, string pageid, string username, string password, Page page, bool publish)
    {
      _logger.LogInformation($"wp.editPage is called");
      return await _provider.EditPageAsync(blogid, pageid, username, password, page, publish);
    }

    [XmlRpcMethod("wp.deletePage")]
    public async Task<bool> DeletePageAsync(string blogid, string username, string password, string pageid)
    {
      _logger.LogInformation($"wp.deletePage is called");
      return await _provider.DeletePageAsync(blogid, username, password, pageid);
    }
  }
}