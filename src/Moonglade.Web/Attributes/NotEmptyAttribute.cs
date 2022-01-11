using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Attributes;

// https://andrewlock.net/creating-an-empty-guid-validation-attribute/
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class NotEmptyAttribute : ValidationAttribute
{
    public const string DefaultErrorMessage = "The {0} field must not be empty";
    public NotEmptyAttribute() : base(DefaultErrorMessage) { }

    public override bool IsValid(object value)
    {
        // NotEmpty doesn't necessarily mean required
        if (value is null) return true;

        return value switch
        {
            Guid guid => guid != Guid.Empty,
            _ => true
        };
    }
}