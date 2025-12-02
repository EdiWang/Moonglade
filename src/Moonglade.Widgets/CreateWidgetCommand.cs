using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Widgets;

public class CreateWidgetCommand : ICommand<Guid>
{
    [Required]
    [Display(Name = "Title")]
    [MaxLength(128)]
    public string Title { get; set; }

    [Required]
    [Display(Name = "Widget Type")]
    [MaxLength(64)]
    public string WidgetType { get; set; }

    [Display(Name = "Display Order")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; } = true;
}

public class CreateWidgetCommandHandler(
    MoongladeRepository<WidgetEntity> widgetRepo,
    ILogger<CreateWidgetCommandHandler> logger) : ICommandHandler<CreateWidgetCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateWidgetCommand request, CancellationToken ct)
    {
        var widget = new WidgetEntity
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            WidgetType = request.WidgetType.Trim(),
            DisplayOrder = request.DisplayOrder,
            IsEnabled = request.IsEnabled,
            CreatedTimeUtc = DateTime.UtcNow
        };

        await widgetRepo.AddAsync(widget, ct);

        logger.LogInformation("Widget created: {WidgetId}", widget.Id);

        return widget.Id;
    }
}