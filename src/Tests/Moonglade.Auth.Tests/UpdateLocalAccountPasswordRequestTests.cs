using System.ComponentModel.DataAnnotations;

namespace Moonglade.Auth.Tests;

public class UpdateLocalAccountPasswordRequestTests
{
    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Valid_Request_PassesValidation()
    {
        var request = new UpdateLocalAccountPasswordRequest
        {
            OldPassword = "Password1",
            NewPassword = "Password2"
        };

        var results = ValidateModel(request);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Missing_Passwords_FailForTheirOwnField(bool empty)
    {
        var password = empty ? "" : null;
        var request = new UpdateLocalAccountPasswordRequest
        {
            OldPassword = password,
            NewPassword = password
        };

        var results = ValidateModel(request);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(UpdateLocalAccountPasswordRequest.OldPassword)));
        Assert.Contains(ValidateModel(new ResetLocalAccountTotpRequest { CurrentPassword = password }),
            result => result.MemberNames.Contains(nameof(ResetLocalAccountTotpRequest.CurrentPassword)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(UpdateLocalAccountPasswordRequest.NewPassword)));
    }

    [Theory]
    [InlineData("missing-digit")]
    [InlineData("missing-letter")]
    [InlineData("short")]
    [InlineData("null")]
    [InlineData("empty")]
    [InlineData("space")]
    [InlineData("leading-space")]
    [InlineData("trailing-space")]
    [InlineData("control")]
    [InlineData("non-ascii")]
    public void Invalid_NewPassword_FailsValidation(string scenario)
    {
        var password = InvalidPassword(scenario);
        var request = new UpdateLocalAccountPasswordRequest
        {
            OldPassword = "Password1",
            NewPassword = password
        };

        var results = ValidateModel(request);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(UpdateLocalAccountPasswordRequest.NewPassword)));
    }

    [Theory]
    [InlineData("missing-digit")]
    [InlineData("missing-letter")]
    [InlineData("short")]
    [InlineData("null")]
    [InlineData("empty")]
    [InlineData("space")]
    [InlineData("leading-space")]
    [InlineData("trailing-space")]
    [InlineData("control")]
    [InlineData("non-ascii")]
    public void Invalid_OldPassword_FailsValidation(string scenario)
    {
        var password = InvalidPassword(scenario);
        var request = new UpdateLocalAccountPasswordRequest
        {
            OldPassword = password,
            NewPassword = "Password2"
        };

        var results = ValidateModel(request);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(UpdateLocalAccountPasswordRequest.OldPassword)));
    }

    [Theory]
    [InlineData("Abcdefg1")]
    [InlineData("MyP4ssword")]
    [InlineData("Test1234")]
    [InlineData("a1b2c3d4e5")]
    [InlineData("nY8qR-v:3m_.!pK")]
    [InlineData("a1!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~")]
    public void Valid_Passwords_PassValidation(string password)
    {
        var request = new UpdateLocalAccountPasswordRequest
        {
            OldPassword = password,
            NewPassword = password
        };

        var results = ValidateModel(request);
        Assert.Empty(results);
        Assert.Empty(ValidateModel(new ResetLocalAccountTotpRequest { CurrentPassword = password }));
    }

    private static string InvalidPassword(string scenario) => scenario switch
    {
        "missing-digit" => "abcdefgh",
        "missing-letter" => "12345678",
        "short" => "Pass1",
        "space" => "Pass word1",
        "leading-space" => " Password1",
        "trailing-space" => "Password1 ",
        "control" => "Password1\0",
        "non-ascii" => "Password1é",
        "null" => null,
        _ => ""
    };
}
