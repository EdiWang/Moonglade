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
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(UpdateLocalAccountPasswordRequest.NewPassword)));
    }

    [Theory]
    [InlineData("missing-digit")]
    [InlineData("missing-letter")]
    [InlineData("short")]
    [InlineData("null")]
    [InlineData("empty")]
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
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Valid_Passwords_PassValidation(int scenario)
    {
        var password = new[] { "Abcdefg1", "MyP4ssword", "Test1234", "a1b2c3d4e5" }[scenario];
        var request = new UpdateLocalAccountPasswordRequest
        {
            OldPassword = password,
            NewPassword = password
        };

        var results = ValidateModel(request);
        Assert.Empty(results);
    }

    private static string InvalidPassword(string scenario) => scenario switch
    {
        "missing-digit" => "abcdefgh",
        "missing-letter" => "12345678",
        "short" => "Pass1",
        "null" => null,
        _ => ""
    };
}
