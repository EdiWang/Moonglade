using Moonglade.Core;

namespace Moonglade.Web.Models;

public class CheckNewReleaseResult
{
    public bool HasNewRelease { get; set; }

    public ReleaseInfo LatestReleaseInfo { get; set; }
    public string CurrentAssemblyFileVersion { get; set; }
}