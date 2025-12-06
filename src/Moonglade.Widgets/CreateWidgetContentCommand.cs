using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Widgets;

public class CreateWidgetContentCommand : ICommand<Guid>
{
    [Required]
    public Guid WidgetId { get; set; }

    [Required]
    [MaxLength(256)]
    public string Title { get; set; }

    [Required]
    public WidgetContentType ContentType { get; set; }

    [Required]
    public string ContentCode { get; set; }
}

public class CreateWidgetContentCommandHandler(
    MoongladeRepository<WidgetContentEntity> repo,
    ILogger<CreateWidgetContentCommandHandler> logger) : ICommandHandler<CreateWidgetContentCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateWidgetContentCommand request, CancellationToken ct)
    {
        var widgetContent = new WidgetContentEntity
        {
            Id = Guid.NewGuid(),
            WidgetId = request.WidgetId,
            Title = request.Title.Trim(),
            ContentType = request.ContentType,
            ContentCode = request.ContentCode.Trim()
        };

        await repo.AddAsync(widgetContent, ct);

        logger.LogInformation("Widget content created: {WidgetContentId} for Widget: {WidgetId}", widgetContent.Id, request.WidgetId);

        return widgetContent.Id;
    }
}