using System.Collections.Generic;

namespace Moonglade.Auth
{
    public class AuthenticationSettings
    {
        public AuthenticationProvider Provider { get; set; }

        public AzureAdOption AzureAd { get; set; }

        public IReadOnlyCollection<ApiKey> ApiKeys { get; set; }

        public MetaWeblogCredential MetaWeblog { get; set; }

        public AuthenticationSettings()
        {
            Provider = AuthenticationProvider.None;
        }
    }

    public class MetaWeblogCredential
    {
        public string Password { get; set; }
    }
}
