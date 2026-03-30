namespace Moonglade.Data.Specifications;

public record CommentFilter(
    string Username = null,
    string Email = null,
    string CommentContent = null,
    DateTime? StartTimeUtc = null,
    DateTime? EndTimeUtc = null);
