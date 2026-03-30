using LiteBus.Commands.Abstractions;
using Moonglade.Data.DTO;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Exporting;

public record ExportPostDataCommand : ICommand<ExportResult>;

public class ExportPostDataCommandHandler(BlogDbContext db) : ICommandHandler<ExportPostDataCommand, ExportResult>
{
    public async Task<ExportResult> HandleAsync(ExportPostDataCommand request, CancellationToken ct)
    {
        var data = await db.Post
            .AsNoTracking()
            .Select(p => new PostExportModel
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                RouteLink = p.RouteLink,
                Author = p.Author,
                ContentAbstract = p.ContentAbstract,
                PostContent = p.PostContent,
                CreateTimeUtc = p.CreateTimeUtc,
                LastModifiedUtc = p.LastModifiedUtc,
                ScheduledPublishTimeUtc = p.ScheduledPublishTimeUtc,
                CommentEnabled = p.CommentEnabled,
                PubDateUtc = p.PubDateUtc,
                ContentLanguageCode = p.ContentLanguageCode,
                IsDeleted = p.IsDeleted,
                IsFeedIncluded = p.IsFeedIncluded,
                IsFeatured = p.IsFeatured,
                PostStatus = p.PostStatus,
                IsOutdated = p.IsOutdated,
                Keywords = p.Keywords,
                Categories = p.PostCategory.Select(pc => pc.Category.DisplayName).ToList(),
                Tags = p.Tags.Select(t => t.DisplayName).ToList()
            })
            .ToListAsync(ct);

        var exporter = new ZippedJsonExporter("moonglade-posts", Path.GetTempPath());
        return await exporter.ExportData(data, ct);
    }
}