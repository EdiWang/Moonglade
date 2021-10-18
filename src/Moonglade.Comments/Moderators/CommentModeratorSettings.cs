namespace Moonglade.Comments.Moderators;

public class CommentModeratorSettings
{
    public string Provider { get; set; }

    public AzureContentModeratorSettings AzureContentModeratorSettings { get; set; }
}