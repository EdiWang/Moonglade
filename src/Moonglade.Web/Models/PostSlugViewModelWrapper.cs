using Moonglade.Model;

namespace Moonglade.Web.Models
{
    public class PostSlugViewModelWrapper
    {
        public PostSlug PostModel { get; }
        public NewCommentViewModel NewCommentViewModel { get; set; }

        public PostSlugViewModelWrapper()
        {
            // Work around for System.InvalidOperationException
            // Model bound complex types must not be abstract or value types and must have a parameterless constructor. Alternatively, give the 'model' parameter a non-null default value.
        }

        public PostSlugViewModelWrapper(PostSlug postModel)
        {
            PostModel = postModel;
            NewCommentViewModel = new NewCommentViewModel { PostId = PostModel.Id };
        }
    }
}
