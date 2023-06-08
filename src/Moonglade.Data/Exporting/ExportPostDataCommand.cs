using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Exporting.Exporters;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Exporting;

public record ExportPostDataCommand : IRequest<ExportResult>;

public class ExportPostDataCommandHandler : IRequestHandler<ExportPostDataCommand, ExportResult>
{
    private readonly IRepository<PostEntity> _repo;
    public ExportPostDataCommandHandler(IRepository<PostEntity> repo) => _repo = repo;

    public Task<ExportResult> Handle(ExportPostDataCommand request, CancellationToken ct)
    {
        var poExp = new ZippedJsonExporter<PostEntity>(_repo, "moonglade-posts", ExportManager.DataDir);
        var poExportData = poExp.ExportData(p => new
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
            p.IsPublished,
            Categories = p.PostCategory.Select(pc => pc.Category.DisplayName),
            Tags = p.Tags.Select(pt => pt.DisplayName)
        }, ct);

        return poExportData;
    }
}