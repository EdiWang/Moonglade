namespace Moonglade.Comments.Moderators;

public interface ICommentModerator
{
    public Task<string> ModerateContent(string input);

    public Task<bool> HasBadWord(params string[] input);
}