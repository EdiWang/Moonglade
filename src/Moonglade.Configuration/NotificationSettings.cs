using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public sealed record NotificationSettings : IBlogSettings<NotificationSettings>
{
    [Display(Name = "Enable email sending")]
    public bool EnableEmailSending { get; init; }

    [Display(Name = "Send email on comment reply")]
    public bool SendEmailOnCommentReply { get; init; }

    [Display(Name = "Send email on new comment")]
    public bool SendEmailOnNewComment { get; init; }

    [Display(Name = "Display name")]
    [MaxLength(64)]
    public string EmailDisplayName { get; init; }

    [JsonIgnore]
    public static NotificationSettings DefaultValue => new()
    {
        EnableEmailSending = false,
        SendEmailOnCommentReply = false,
        SendEmailOnNewComment = false,
        EmailDisplayName = "Moonglade"
    };
}
