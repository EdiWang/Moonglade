using System.Reflection;
using System.Text.RegularExpressions;

namespace Moonglade.Utils;

public static partial class VersionHelper
{
    private static readonly Assembly _entryAssembly = Assembly.GetEntryAssembly();
    private static readonly string _fileVersion = _entryAssembly?.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
    private static readonly string _informationalVersion = _entryAssembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    [GeneratedRegex(@"\b(preview|beta|rc|debug|alpha|test|canary|nightly)\b", RegexOptions.IgnoreCase)]
    private static partial Regex NonStableVersionRegex();

    public static string AppVersionBasic => _fileVersion ?? "N/A";

    public static string AppVersion
    {
        get
        {
            if (_informationalVersion is null)
                return AppVersionBasic;

            var plusIndex = _informationalVersion.IndexOf('+');
            if (plusIndex <= 0)
                return _informationalVersion;

            var gitHash = _informationalVersion.AsSpan()[(plusIndex + 1)..];
            var prefix = _informationalVersion.AsSpan()[..plusIndex];

            if (gitHash.Length <= 6)
                return _informationalVersion;

            var gitHashShort = gitHash[..6];
            return gitHashShort.IsEmpty ? AppVersionBasic : $"{prefix} ({gitHashShort})";
        }
    }

    public static bool IsNonStableVersion() => NonStableVersionRegex().IsMatch(AppVersion);
}
