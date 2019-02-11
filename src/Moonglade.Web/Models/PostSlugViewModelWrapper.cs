namespace Moonglade.Web.Models
{
    public class PostSlugViewModelWrapper
    {
        public PostSlugViewModel PostModel { get; set; }
        public CommentPostModel CommentPostModel { get; set; }

        public PostSlugViewModelWrapper()
        {
            CommentPostModel = new CommentPostModel();
        }
    }
}
