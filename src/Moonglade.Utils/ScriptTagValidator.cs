using System.Text.RegularExpressions;

namespace Moonglade.Utils;

public static partial class ScriptTagValidator
{
    /// <summary>
    /// Validates that the input contains only valid script blocks and whitespace.
    /// Multiple script tags are allowed.
    /// </summary>
    public static bool IsValidScriptBlock(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return true;

        // Strip all <script ...>...</script> blocks (including attributes like src, async, etc.)
        var stripped = ScriptBlockRegex().Replace(input, string.Empty);

        // After removing all script blocks, only whitespace should remain
        return string.IsNullOrWhiteSpace(stripped);
    }

    // Matches <script> tags with optional attributes, across multiple lines
    [GeneratedRegex(@"<script\b[^>]*>[\s\S]*?</script>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptBlockRegex();
}
