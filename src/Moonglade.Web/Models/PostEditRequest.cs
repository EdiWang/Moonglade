using Moonglade.Features.Post;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models;

public sealed class PostEditRequest : PostEditModel
{
    [Display(Name = "Scheduled Publish Time")]
    [DataType(DataType.DateTime)]
    public DateTime? ScheduledPublishLocalTime { get; set; }

    public string ClientTimeZoneId { get; set; }

    public DateTimeOffset? LastModifiedUtc { get; set; }
}

public sealed record SavePostResponse(Guid PostId, DateTime? LastModifiedUtc);
