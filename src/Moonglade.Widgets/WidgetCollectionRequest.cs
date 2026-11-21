#nullable enable

using Moonglade.Data.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Moonglade.Widgets;

public sealed class WidgetCollectionRequest : IValidatableObject
{
    [JsonRequired]
    [Required]
    public List<WidgetDraft?>? Widgets { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Widgets is null) yield break;
        var ids = new HashSet<Guid>();
        for (var index = 0; index < Widgets.Count; index++)
        {
            var widget = Widgets[index];
            var path = $"Widgets[{index}]";
            if (widget is null)
            {
                yield return new ValidationResult("A complete widget is required.", [path]);
                continue;
            }

            if (widget.Id == Guid.Empty || !ids.Add(widget.Id))
                yield return new ValidationResult("Each widget requires a unique nonempty ID.", [$"{path}.Id"]);

            List<ValidationResult> errors = [];
            var edit = widget.ToEditRequest();
            Validator.TryValidateObject(edit, new ValidationContext(edit), errors, true);
            foreach (var error in errors)
                yield return new ValidationResult($"Widget {widget.Id}: {error.ErrorMessage}",
                    error.MemberNames.Select(member => $"{path}.{member}"));
        }
    }
}

public sealed class WidgetDraft
{
    [JsonRequired]
    public Guid Id { get; set; }
    [JsonRequired]
    public string? Title { get; set; }
    [JsonRequired]
    public WidgetType WidgetType { get; set; }
    [JsonRequired]
    public WidgetRegion Region { get; set; }
    [JsonRequired]
    public int SortOrder { get; set; }
    [JsonRequired]
    public bool IsEnabled { get; set; }
    [JsonRequired]
    public string? ContentCode { get; set; }

    public EditWidgetRequest ToEditRequest() => new()
    {
        Title = Title!, WidgetType = WidgetType, Region = Region,
        SortOrder = SortOrder, IsEnabled = IsEnabled, ContentCode = ContentCode!
    };
}
