using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Moonglade.Utils;

public static class Helper
{
    // Get `sec-ch-prefers-color-scheme` header value
    // This is to enhance user experience by stopping the screen from blinking when switching pages
    public static bool IsClientPreferDarkMode(HttpContext context)
    {
        bool useServerSideDarkMode = false;
        var prefersColorScheme = context.Request.Headers["Sec-CH-Prefers-Color-Scheme"];
        if (prefersColorScheme == "dark")
        {
            useServerSideDarkMode = true;
        }

        return useServerSideDarkMode;
    }

    public static string FormatCopyright2Html(string copyrightCode)
    {
        if (string.IsNullOrWhiteSpace(copyrightCode))
        {
            return copyrightCode;
        }

        var result = copyrightCode.Replace("[c]", "&copy;")
            .Replace("[year]", DateTime.UtcNow.Year.ToString());
        return result;
    }

    public static bool TryParseBase64(string input, out byte[] base64Array)
    {
        base64Array = null;

        if (string.IsNullOrWhiteSpace(input) ||
            input.Length % 4 != 0 ||
            !Regex.IsMatch(input, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None))
        {
            return false;
        }

        try
        {
            base64Array = Convert.FromBase64String(input);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static bool IsValidEmailAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true; // Assuming null/empty is valid
        }

        // Simple validation: must contain exactly one @ with characters before and after
        int atIndex = value.IndexOf('@');
        return atIndex > 0 &&
               atIndex < value.Length - 1 &&
               value.IndexOf('@', atIndex + 1) == -1;
    }

    public static void ValidatePagingParameters(int pageSize, int pageIndex)
    {
        if (pageSize is < 1 or > 1024)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize),
                $"{nameof(pageSize)} out of range, current value: {pageSize}.");
        }

        if (pageIndex is < 1 or > 1024)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex),
                $"{nameof(pageIndex)} out of range, current value: {pageIndex}.");
        }
    }

    public static string GetMagic(int value, int start, int end) =>
        Convert.ToBase64String(SHA256.HashData(BitConverter.GetBytes(value)))[start..end];
}