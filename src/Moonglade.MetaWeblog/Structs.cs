// ReSharper disable InconsistentNaming
namespace Moonglade.MetaWeblog;

public class BlogInfo
{
    public string blogid;
    public string url;
    public string blogName;
}

public class CategoryInfo
{
    public string description;
    public string htmlUrl;
    public string rssUrl;
    public string title;
    public string categoryid;
}

public class NewCategory
{
    public string name;
    public int parent_id;
    public string slug;
    public string description;
}

public class Tag
{
    public string name;
}

public class Enclosure
{
    public int length;
    public string type;
    public string url;
}

public class Post
{
    public DateTime dateCreated;
    public string description;
    public string title;
    public string[] categories;
    public string permalink;
    public object postid;
    public string userid;
    public string wp_slug;
    public string mt_excerpt;
    public string mt_keywords;
    public string link;
    public string wp_post_thumbnail;
    public int mt_allow_comments;
    public string mt_basename;
}

public class Source
{
    public string name;
    public string url;
}

public class UserInfo
{
    public string userid;
    public string firstname;
    public string lastname;
    public string nickname;
    public string email;
    public string url;
}

public class MediaObject
{
    public string name;
    public string type;
    public string bits;
}

public class MediaObjectInfo
{
    public string url;
}

public class Page
{
    public DateTime dateCreated;
    public string description;
    public string title;
    public string[] categories;
    public string wp_author_id;
    public string page_parent_id;
    public string page_id;
}

public class Author
{
    public string user_id;
    public string user_login;
    public string display_name;
    public string meta_value;
}