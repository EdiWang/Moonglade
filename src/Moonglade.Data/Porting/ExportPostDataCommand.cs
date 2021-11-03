using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Porting.Exporters;

namespace Moonglade.Data.Porting;

public class ExportPostDataCommand : IRequest<ExportResult>
{
}

public class ExportPostDataCommandHandler : IRequestHandler<ExportPostDataCommand, ExportResult>
{
    private readonly IRepository<PostEntity> _postRepository;

    public ExportPostDataCommandHandler(IRepository<PostEntity> postRepository)
    {
        _postRepository = postRepository;
    }

    public Task<ExportResult> Handle(ExportPostDataCommand request, CancellationToken cancellationToken)
    {
        var poExp = new ZippedJsonExporter<PostEntity>(_postRepository, "moonglade-posts", ExportManager.DataDir);
        var poExportData = poExp.ExportData(p => new
        {
            p.Title,
            p.Slug,
            p.ContentAbstract,
            p.PostContent,
            p.CreateTimeUtc,
            p.CommentEnabled,
            p.PostExtension.Hits,
            p.PostExtension.Likes,
            p.PubDateUtc,
            p.ContentLanguageCode,
            p.IsDeleted,
            p.IsFeedIncluded,
            p.IsPublished,
            Categories = p.PostCategory.Select(pc => pc.Category.DisplayName),
            Tags = p.Tags.Select(pt => pt.DisplayName)
        }, cancellationToken);

        return poExportData;
    }
}