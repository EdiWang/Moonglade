using System.ComponentModel.DataAnnotations;

namespace Moonglade.Configuration.Tests;

public class ImageSettingsTests
{
    [Fact]
    public void Validate_MissingCdnEndpoint_ReportsErrorsWithoutChangingInput()
    {
        var settings = ImageSettings.DefaultValue;
        settings.EnableCDNRedirect = true;
        var original = settings.ToJson();
        var errors = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(settings, new ValidationContext(settings), errors, true);

        Assert.False(valid);
        Assert.NotEmpty(errors);
        Assert.Equal(original, settings.ToJson());
    }
}
