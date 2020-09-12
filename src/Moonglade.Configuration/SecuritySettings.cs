using Moonglade.Configuration.Abstraction;

namespace Moonglade.Configuration
{
    public class SecuritySettings : MoongladeSettings
    {
        public bool WarnExternalLink { get; set; }
        public bool AllowScriptsInCustomPage { get; set; }
        public bool ShowAdminLoginButton { get; set; }
        public bool EnablePostRawEndpoint { get; set; }
    }
}
