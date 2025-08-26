using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Security.Claims;

namespace Moonglade.Web.TagHelpers;

public enum UserInfoDisplay
{
    PreferName,
    PreferEmail,
    Both
}

[HtmlTargetElement("userinfo", TagStructure = TagStructure.NormalOrSelfClosing)]
public class UserInfoTagHelper : TagHelper
{
    public ClaimsPrincipal User { get; set; }

    public UserInfoDisplay UserInfoDisplay { get; set; } = UserInfoDisplay.Both;

    public static string TagClassBase => "aspnet-tag-moonglade-userinfo";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (User?.Identity is null || !User.Identity.IsAuthenticated)
        {
            base.Process(context, output);
            return;
        }

        var name = GetName();
        var email = GetEmail();

        output.TagName = "div";
        output.Attributes.SetAttribute("class", TagClassBase);

        switch (UserInfoDisplay)
        {
            case UserInfoDisplay.PreferName:
                output.Content.SetContent(name ?? email ?? string.Empty);
                break;
            case UserInfoDisplay.PreferEmail:
                output.Content.SetContent(email ?? name ?? string.Empty);
                break;
            case UserInfoDisplay.Both:
                SetBothDisplayContent(output, name, email);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(UserInfoDisplay), UserInfoDisplay, null);
        }
    }

    private void SetBothDisplayContent(TagHelperOutput output, string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(email))
        {
            output.Content.SetContent(string.Empty);
            return;
        }

        var nameHtml = !string.IsNullOrWhiteSpace(name)
            ? $"<div class='{TagClassBase}-name'>{name}</div>"
            : string.Empty;

        var emailHtml = !string.IsNullOrWhiteSpace(email)
            ? $"<div class='{TagClassBase}-email'>{email}</div>"
            : string.Empty;

        output.Content.SetHtmlContent($"{nameHtml}{emailHtml}");
    }

    private string GetName()
    {
        // First try standard Identity.Name
        if (!string.IsNullOrWhiteSpace(User.Identity?.Name))
        {
            return User.Identity.Name;
        }

        // Then try custom "name" claim (case-insensitive)
        var nameClaim = User.Claims.FirstOrDefault(c =>
            string.Equals(c.Type, "name", StringComparison.OrdinalIgnoreCase));

        return nameClaim?.Value;
    }

    private string GetEmail()
    {
        // First try standard email claim
        var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
        if (!string.IsNullOrWhiteSpace(emailClaim?.Value))
        {
            return emailClaim.Value;
        }

        // Then try custom "email" claim (case-insensitive)
        var customEmailClaim = User.Claims.FirstOrDefault(c =>
            string.Equals(c.Type, "email", StringComparison.OrdinalIgnoreCase));

        return customEmailClaim?.Value;
    }
}