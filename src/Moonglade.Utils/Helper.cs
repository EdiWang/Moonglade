using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Moonglade.Utils;

public static class Helper
{
    public static void SetAppDomainData(string key, object value)
    {
        AppDomain.CurrentDomain.SetData(key, value);
    }

    public static T GetAppDomainData<T>(string key, T defaultValue = default(T))
    {
        object data = AppDomain.CurrentDomain.GetData(key);
        if (data == null)
        {
            return defaultValue;
        }

        return (T)data;
    }

    // Get `sec-ch-prefers-color-scheme` header value
    // This is to enhance user experience by stopping the screen from blinking when switching pages
    public static bool UseServerSideDarkMode(IConfiguration configuration, HttpContext context)
    {
        bool useServerSideDarkMode = false;
        bool usePrefersColorSchemeHeader = configuration.GetValue<bool>("PrefersColorSchemeHeader:Enabled");
        var prefersColorScheme = context.Request.Headers[configuration["PrefersColorSchemeHeader:HeaderName"]!];
        if (usePrefersColorSchemeHeader && prefersColorScheme == "dark")
        {
            useServerSideDarkMode = true;
        }

        return useServerSideDarkMode;
    }

    public static string GetClientIP(HttpContext context) => context?.Connection.RemoteIpAddress?.ToString();

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
        if (value == null)
        {
            return true;
        }

        var regEx = CreateEmailRegEx();
        if (regEx != null)
        {
            return regEx.Match(value).Length > 0;
        }

        int atCount = value.Count(c => c == '@');

        return (atCount == 1
                && value[0] != '@'
                && value[^1] != '@');
    }

    private static Regex CreateEmailRegEx()
    {
        const string pattern = @"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$";
        const RegexOptions options = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;

        // Set explicit regex match timeout, sufficient enough for email parsing
        // Unless the global REGEX_DEFAULT_MATCH_TIMEOUT is already set
        var matchTimeout = TimeSpan.FromSeconds(2);

        try
        {
            return new(pattern, options, matchTimeout);
        }
        catch
        {
            // Fallback on error
        }

        // Legacy fallback (without explicit match timeout)
        return new(pattern, options);
    }

    public static string GetCombinedErrorMessage(this ModelStateDictionary modelStateDictionary, string sep = ", ")
    {
        var messages = modelStateDictionary.GetErrorMessages();
        var enumerable = messages as string[] ?? [.. messages];
        return enumerable.Length != 0 ? string.Join(sep, enumerable) : string.Empty;
    }

    public static IEnumerable<string> GetErrorMessages(this ModelStateDictionary modelStateDictionary)
    {
        if (modelStateDictionary is null) return null;
        if (modelStateDictionary.ErrorCount == 0) return null;

        return from modelState in modelStateDictionary.Values
               from error in modelState.Errors
               select error.ErrorMessage;
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