using LiteBus.Commands.Abstractions;
using MediatR;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Exporting;

public record ExportPostDataCommand : ICommand<ExportResult>;

public class ExportPostDataCommandHandler(MoongladeRepository<PostEntity> repo) : ICommandHandler<ExportPostDataCommand, ExportResult>
{
    public Task<ExportResult> HandleAsync(ExportPostDataCommand request, CancellationToken ct)
    {
        var exporter = new ZippedJsonExporter<PostEntity>(repo, "moonglade-posts", Path.GetTempPath());
        var data = exporter.ExportData(p => new
        {
            p.Id,
            p.Title,
            p.Slug,
            p.RouteLink,
            p.Author,
            p.ContentAbstract,
            p.PostContent,
            p.HeroImageUrl,
            p.CreateTimeUtc,
            p.LastModifiedUtc,
            p.ScheduledPublishTimeUtc,
            p.CommentEnabled,
            p.PubDateUtc,
            p.ContentLanguageCode,
            p.IsDeleted,
            p.IsFeedIncluded,
            p.IsFeatured,
            p.PostStatus,
            p.IsOutdated,
            Categories = p.PostCategory.Select(pc => pc.Category.DisplayName),
            Tags = p.Tags.Select(pt => pt.DisplayName)
        }, ct);

        return data;
    }
}