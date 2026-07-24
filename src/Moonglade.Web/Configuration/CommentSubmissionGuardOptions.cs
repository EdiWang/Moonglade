using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Configuration;

public class CommentSubmissionGuardOptions
{
    public const string SectionName = "CommentSubmissionGuard";

    public bool Enabled { get; set; } = true;

    public bool HoneypotEnabled { get; set; } = true;

    [Range(0, int.MaxValue)]
    public int MinimumElapsedSeconds { get; set; } = 3;

    [Range(0, int.MaxValue)]
    public int MaxFormAgeMinutes { get; set; } = 240;
}
