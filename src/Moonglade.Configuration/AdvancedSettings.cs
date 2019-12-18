using Moonglade.Configuration.Abstraction;

namespace Moonglade.Configuration
{
    public class AdvancedSettings : MoongladeSettings
    {
        public string DNSPrefetchEndpoint { get; set; }

        public bool EnablePingBackSend { get; set; }

        public bool EnablePingBackReceive { get; set; }
    }
}
