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
            NewUsername = "newadmin",
            OldPassword = "Password1",
            NewPassword = "Password2"
        };

        var results = ValidateModel(request);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]          // too short
    [InlineData("user@name")]   // invalid char
    [InlineData("12345678901234567")] // 17 chars, too long
    public void Invalid_Username_FailsValidation(string username)
    {
        var request = new UpdateLocalAccountPasswordRequest
        {
            NewUsername = username,
            OldPassword = "Password1",
            NewPassword = "Password2"
        };

        var results = ValidateModel(request);
        Assert.NotEmpty(results);
    }

    [Theory]
    [InlineData("abc")]         // only letters, no digit
    [InlineData("12345678")]    // only digits, no letter
    [InlineData("Pass1")]       // too short (< 8)
    [InlineData(null)]
    [InlineData("")]
    public void Invalid_NewPassword_FailsValidation(string password)
    {
        var request = new UpdateLocalAccountPasswordRequest
        {
            NewUsername = "admin",
            OldPassword = "Password1",
            NewPassword = password
        };

        var results = ValidateModel(request);
        Assert.NotEmpty(results);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12345678")]
    [InlineData("Pass1")]
    [InlineData(null)]
    [InlineData("")]
    public void Invalid_OldPassword_FailsValidation(string password)
    {
        var request = new UpdateLocalAccountPasswordRequest
        {
            NewUsername = "admin",
            OldPassword = password,
            NewPassword = "Password2"
        };

        var results = ValidateModel(request);
        Assert.NotEmpty(results);
    }

    [Theory]
    [InlineData("Abcdefg1")]
    [InlineData("MyP4ssword")]
    [InlineData("Test1234")]
    [InlineData("a1b2c3d4e5")]
    public void Valid_Passwords_PassValidation(string password)
    {
        var request = new UpdateLocalAccountPasswordRequest
        {
            NewUsername = "admin",
            OldPassword = password,
            NewPassword = password
        };

        var results = ValidateModel(request);
        Assert.Empty(results);
    }
}
