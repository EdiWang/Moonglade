using Microsoft.Win32;
using System.Reflection;
using System.Runtime.InteropServices;
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

    public static string TryGetFullOSVersion()
    {
        var osVer = Environment.OSVersion;
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return osVer.VersionString;

        try
        {
            var currentVersion = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (currentVersion != null)
            {
                var name = currentVersion.GetValue("ProductName", "Microsoft Windows NT");
                var ubr = currentVersion.GetValue("UBR", string.Empty).ToString();
                if (!string.IsNullOrWhiteSpace(ubr))
                {
                    return $"{name} {osVer.Version.Major}.{osVer.Version.Minor}.{osVer.Version.Build}.{ubr}";
                }
            }
        }
        catch
        {
            return osVer.VersionString;
        }

        return osVer.VersionString;
    }
}
