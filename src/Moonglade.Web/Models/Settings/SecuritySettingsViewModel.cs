using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Moonglade.Web.Models.Settings
{
    public class SecuritySettingsViewModel
    {
        public bool WarnExternalLink { get; set; }
        public bool AllowScriptsInCustomPage { get; set; }
        public bool ShowAdminLoginButton { get; set; }
        public bool EnablePostRawEndpoint { get; set; }
    }
}
