using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Security.Cryptography;
using System.Text.Encodings.Web;

namespace Moonglade.Web.TagHelpers;

[HtmlTargetElement("gravatar", TagStructure = TagStructure.NormalOrSelfClosing)]
public class GravatarImgHelper : TagHelper
{
    public string Email { get; set; }

    public int Size { get; set; } = 58;

    public string DefaultImageUrl { get; set; } = string.Empty;

    public bool PreferHttps { get; set; } = true;

    public bool ForceDefaultImage { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var email = string.IsNullOrEmpty(Email) ? string.Empty : Email.Trim().ToLower();
        var emailHash = GetMd5Hash(email);

        var src = string.Format("{0}://{1}.gravatar.com/avatar/{2}?s={3}{4}{5}{6}",
            PreferHttps ? "https" : "http",
            PreferHttps ? "secure" : "www",
            emailHash,
            Size.ToString(),
            "&d=" + (!string.IsNullOrEmpty(DefaultImageUrl)
                ? UrlEncoder.Default.Encode(DefaultImageUrl)
                : string.Empty),
            ForceDefaultImage ? "&f=y" : string.Empty,
            "&r=g");

        output.TagName = "img";
        output.Attributes.SetAttribute("src", src);
        output.Attributes.SetAttribute("alt", "Gravatar image");
    }

    private static string GetMd5Hash(string input)
    {
        // Convert the input string to a byte array and compute the hash.
        var data = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(input));

        // Create a new Stringbuilder to collect the bytes
        // and create a string.
        var sBuilder = new StringBuilder();

        // Loop through each byte of the hashed data
        // and format each one as a hexadecimal string.
        foreach (var t in data)
        {
            sBuilder.Append(t.ToString("x2"));
        }

        // Return the hexadecimal string.
        return sBuilder.ToString();
    }
}