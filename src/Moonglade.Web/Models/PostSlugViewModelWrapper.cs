namespace Moonglade.Web.Models
{
    public class PostSlugViewModelWrapper
    {
        public PostSlugViewModel PostModel { get; set; }
        public NewCommentModel NewCommentModel { get; set; }

        public PostSlugViewModelWrapper()
        {
            NewCommentModel = new NewCommentModel();
        }
    }
}
