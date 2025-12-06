using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Widgets;

public class DeleteWidgetContentCommand : ICommand
{
    [Required]
    public Guid Id { get; set; }
}

public class DeleteWidgetContentCommandHandler(
    MoongladeRepository<WidgetContentEntity> repo,
    ILogger<DeleteWidgetContentCommandHandler> logger) : ICommandHandler<DeleteWidgetContentCommand>
{
    public async Task HandleAsync(DeleteWidgetContentCommand request, CancellationToken ct)
    {
        var widgetContent = await repo.GetByIdAsync(request.Id, ct);
        
        if (widgetContent is null)
        {
            logger.LogWarning("Widget content not found: {WidgetContentId}", request.Id);
            return;
        }

        await repo.DeleteAsync(widgetContent, ct);

        logger.LogInformation("Widget content deleted: {WidgetContentId}", request.Id);
    }
}