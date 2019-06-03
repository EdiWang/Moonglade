using Moonglade.Model;

namespace Moonglade.Web.Models
{
    public class PostSlugViewModelWrapper
    {
        public PostSlugModel PostModel { get; }
        public NewCommentViewModel NewCommentViewModel { get; set; }

        public PostSlugViewModelWrapper(PostSlugModel postModel)
        {
            PostModel = postModel;
            NewCommentViewModel = new NewCommentViewModel { PostId = PostModel.PostId };
        }
    }
}
