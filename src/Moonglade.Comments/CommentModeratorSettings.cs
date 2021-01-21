namespace Moonglade.Comments
{
    public class CommentModeratorSettings
    {
        public string Provider { get; set; }

        public AzureContentModeratorSettings AzureContentModeratorSettings { get; set; }
    }
}