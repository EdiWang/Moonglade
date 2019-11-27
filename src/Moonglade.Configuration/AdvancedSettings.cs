using Moonglade.Configuration.Abstraction;
using System;
using System.Collections.Generic;
using System.Text;

namespace Moonglade.Configuration
{
    public class AdvancedSettings : MoongladeSettings
    {
        public string DNSPrefetchEndpoint { get; set; }

        public bool EnablePingBackSend { get; set; }

        public bool EnablePingBackReceive { get; set; }
    }
}
