using Moonglade.Data.Exporting;
using Moonglade.Data.Entities;
using Moonglade.Widgets.Types;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Moonglade.Widgets;

public class EditWidgetRequest : IValidatableObject
{
    [Required]
    [Display(Name = "Title")]
    [MaxLength(128)]
    public string Title { get; set; }

    [Required]
    [Display(Name = "Widget Type")]
    public WidgetType WidgetType { get; set; }

    [MaxLength(2000)]
    public string ContentCode { get; set; }

    [Display(Name = "Display Order")]
    [Range(-30, 999)]
    public int DisplayOrder { get; set; }

    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResult = WidgetType switch
        {
            WidgetType.LinkList => ValidateLinkList(ContentCode),
            WidgetType.ImageLink => ValidateImageLink(ContentCode),
            WidgetType.ButtonLink => ValidateButtonLink(ContentCode),
            _ => new ValidationResult("Unsupported widget type.", [nameof(WidgetType)])
        };

        if (validationResult != null)
        {
            yield return validationResult;
        }
    }

    private static ValidationResult ValidateLinkList(string contentCode)
    {
        if (!TryDeserialize(contentCode, out List<LinkListItem> links))
        {
            return InvalidContentCode("Content code must be a JSON array of link list items.");
        }

        if (links.Any(item =>
            item == null ||
            string.IsNullOrWhiteSpace(item.Name) ||
            string.IsNullOrWhiteSpace(item.Url)))
        {
            return InvalidContentCode("Link list items require name and URL.");
        }

        return null;
    }

    private static ValidationResult ValidateImageLink(string contentCode)
    {
        if (!TryDeserialize(contentCode, out ImageLinkData imageLink))
        {
            return InvalidContentCode("Content code must be a JSON image link object.");
        }

        if (string.IsNullOrWhiteSpace(imageLink.ImageUrl))
        {
            return InvalidContentCode("Image link data requires an image URL.");
        }

        return null;
    }

    private static ValidationResult ValidateButtonLink(string contentCode)
    {
        if (!TryDeserialize(contentCode, out List<ButtonLinkItem> buttons))
        {
            return InvalidContentCode("Content code must be a JSON array of button link items.");
        }

        if (buttons.Count is 0 or > 3)
        {
            return InvalidContentCode("Button link data must include one to three buttons.");
        }

        if (buttons.Any(item =>
            item == null ||
            string.IsNullOrWhiteSpace(item.Text) ||
            string.IsNullOrWhiteSpace(item.Url)))
        {
            return InvalidContentCode("Button link items require text and URL.");
        }

        return null;
    }

    private static bool TryDeserialize<T>(string contentCode, out T value)
    {
        value = default;

        if (string.IsNullOrWhiteSpace(contentCode))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<T>(contentCode, MoongladeJsonSerializerOptions.Default);
            return value != null;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    private static ValidationResult InvalidContentCode(string errorMessage) =>
        new(errorMessage, [nameof(ContentCode)]);
}
