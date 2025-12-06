using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Widgets;

public class UpdateWidgetContentCommand : ICommand
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Title { get; set; }

    [Required]
    public WidgetContentType ContentType { get; set; }

    [Required]
    public string ContentCode { get; set; }
}

public class UpdateWidgetContentCommandHandler(
    MoongladeRepository<WidgetContentEntity> repo,
    ILogger<UpdateWidgetContentCommandHandler> logger) : ICommandHandler<UpdateWidgetContentCommand>
{
    public async Task HandleAsync(UpdateWidgetContentCommand request, CancellationToken ct)
    {
        var widgetContent = await repo.GetByIdAsync(request.Id, ct);
        
        if (widgetContent is null)
        {
            logger.LogWarning("Widget content not found: {WidgetContentId}", request.Id);
            return;
        }

        widgetContent.Title = request.Title.Trim();
        widgetContent.ContentType = request.ContentType;
        widgetContent.ContentCode = request.ContentCode.Trim();

        await repo.UpdateAsync(widgetContent, ct);

        logger.LogInformation("Widget content updated: {WidgetContentId}", request.Id);
    }
}