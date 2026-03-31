namespace Moonglade.Data.DTO;

public record CommentFilter(
    string Username = null,
    string Email = null,
    string CommentContent = null,
    DateTime? StartTimeUtc = null,
    DateTime? EndTimeUtc = null);
