using MediatR;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Exporting;

public record ExportPostDataCommand : IRequest<ExportResult>;

public class ExportPostDataCommandHandler(MoongladeRepository<PostEntity> repo) : IRequestHandler<ExportPostDataCommand, ExportResult>
{
    public Task<ExportResult> Handle(ExportPostDataCommand request, CancellationToken ct)
    {
        var exporter = new ZippedJsonExporter<PostEntity>(repo, "moonglade-posts", Path.GetTempPath());
        var data = exporter.ExportData(p => new
        {
            p.Title,
            p.Slug,
            p.ContentAbstract,
            p.PostContent,
            p.CreateTimeUtc,
            p.CommentEnabled,
            p.PubDateUtc,
            p.ContentLanguageCode,
            p.IsDeleted,
            p.IsFeedIncluded,
            p.PostStatus,
            Categories = p.PostCategory.Select(pc => pc.Category.DisplayName),
            Tags = p.Tags.Select(pt => pt.DisplayName)
        }, ct);

        return data;
    }
}