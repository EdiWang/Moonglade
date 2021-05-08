using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moonglade.Core;

namespace Moonglade.Web.Models.Settings
{
    public class CheckNewReleaseResult
    {
        public bool HasNewRelease { get; set; }

        public ReleaseInfo LatestReleaseInfo { get; set; }
        public string CurrentAssemblyFileVersion { get; set; }
    }
}
