using System;
using System.Collections.Generic;
using System.Text;

namespace Moonglade.Model.Settings
{
    public class Authentication
    {
        public string Provider { get; set; }

        public AzureAdInfo AzureAd { get; set; }

        public LocalAccountInfo Local { get; set; }
    }

    public class AzureAdInfo
    {
        public string ClientId { get; set; }

        public string Instance { get; set; }

        public string Domain { get; set; }

        public string TenantId { get; set; }

        public string CallbackPath { get; set; }
    }

    public class LocalAccountInfo
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
