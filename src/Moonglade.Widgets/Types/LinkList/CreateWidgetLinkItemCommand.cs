using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Utils;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Widgets.Types.LinkList;

public class CreateWidgetLinkItemCommand : ICommand<OperationCode>, IValidatableObject
{
    [Required]
    public Guid WidgetId { get; set; }

    [Required]
    [Display(Name = "Title")]
    [MaxLength(64)]
    public string Title { get; set; }

    [Required]
    [Display(Name = "URL")]
    [DataType(DataType.Url)]
    [MaxLength(256)]
    public string Url { get; set; }

    [Display(Name = "Icon Name")]
    [MaxLength(32)]
    public string IconName { get; set; }

    [Display(Name = "Open in New Window")]
    public bool OpenInNewWindow { get; set; } = true;

    [Display(Name = "Display Order")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Uri.IsWellFormedUriString(Url, UriKind.Absolute))
        {
            yield return new($"{nameof(Url)} is not a valid url.");
        }
    }
}

public class CreateWidgetLinkItemCommandHandler(
    MoongladeRepository<WidgetLinkItemEntity> linkItemRepo,
    MoongladeRepository<WidgetEntity> widgetRepo,
    ILogger<CreateWidgetLinkItemCommandHandler> logger) : ICommandHandler<CreateWidgetLinkItemCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(CreateWidgetLinkItemCommand request, CancellationToken ct)
    {
        var widget = await widgetRepo.GetByIdAsync(request.WidgetId, ct);
        if (widget == null)
        {
            return OperationCode.ObjectNotFound;
        }

        var linkItem = new WidgetLinkItemEntity
        {
            Id = Guid.NewGuid(),
            WidgetId = request.WidgetId,
            Title = request.Title.Trim(),
            Url = SecurityHelper.SterilizeLink(request.Url),
            IconName = request.IconName?.Trim() ?? string.Empty,
            OpenInNewWindow = request.OpenInNewWindow,
            DisplayOrder = request.DisplayOrder,
            IsEnabled = request.IsEnabled
        };

        await linkItemRepo.AddAsync(linkItem, ct);

        logger.LogInformation("Widget link item created: {LinkItemId} for widget: {WidgetId}", linkItem.Id, request.WidgetId);

        return OperationCode.Done;
    }
}