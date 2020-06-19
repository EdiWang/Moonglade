using System.Collections.Generic;

namespace Moonglade.Web.Authentication
{
    public class AuthenticationSettings
    {
        public AuthenticationProvider Provider { get; set; }

        public AzureAdOption AzureAd { get; set; }

        public LocalAccountOption Local { get; set; }

        public IReadOnlyCollection<ApiKey> ApiKeys { get; set; }

        public AuthenticationSettings()
        {
            Provider = AuthenticationProvider.None;
        }
    }

    public class ApiKey
    {
        public int Id { get; set; }
        public string Owner { get; set; }
        public string Key { get; set; }
        public IReadOnlyCollection<string> Roles { get; set; }

        public ApiKey()
        {
            Roles = new[] { "Administrator" };
        }
    }
}
