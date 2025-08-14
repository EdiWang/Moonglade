using Microsoft.AspNetCore.Http;

namespace Moonglade.Utils;

public static class ClientIPHelper
{
    public static string GetClientIP(HttpContext context)
    {
        if (context?.Connection?.RemoteIpAddress == null)
            return null;

        // Check for forwarded headers in order of preference
        var forwardedHeaders = new[]
        {
            "X-Azure-ClientIP",        // Azure Front Door
            "CF-Connecting-IP",        // Cloudflare
            "X-Forwarded-For",         // Standard proxy header
            "X-Real-IP",               // Nginx proxy
            "X-Client-IP",             // Apache proxy
            "True-Client-IP",          // Akamai and Cloudflare Enterprise
            "HTTP_X_FORWARDED_FOR",    // IIS
            "HTTP_CLIENT_IP"           // Alternative
        };

        foreach (var header in forwardedHeaders)
        {
            var headerValue = context.Request.Headers[header].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerValue))
            {
                // Handle comma-separated IPs (X-Forwarded-For can contain multiple IPs)
                var ips = headerValue.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var ip in ips)
                {
                    var trimmedIp = ip.Trim();
                    if (IsValidPublicIP(trimmedIp))
                    {
                        return trimmedIp;
                    }
                }
            }
        }

        // Fallback to connection remote IP
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        return IsValidPublicIP(remoteIp) ? remoteIp : remoteIp;
    }

    private static bool IsValidPublicIP(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || !System.Net.IPAddress.TryParse(ipAddress, out var ip))
            return false;

        // Exclude private IP ranges and special addresses
        var bytes = ip.GetAddressBytes();

        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            // IPv4 private ranges
            return !(
                bytes[0] == 10 ||                                    // 10.0.0.0/8
                (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) || // 172.16.0.0/12
                (bytes[0] == 192 && bytes[1] == 168) ||              // 192.168.0.0/16
                (bytes[0] == 169 && bytes[1] == 254) ||              // 169.254.0.0/16 (link-local)
                bytes[0] == 127 ||                                   // 127.0.0.0/8 (loopback)
                bytes[0] == 0                                        // 0.0.0.0/8
            );
        }

        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            // IPv6 private/special ranges
            return !(
                ip.IsIPv6LinkLocal ||
                ip.IsIPv6SiteLocal ||
                ip.IsIPv6Multicast ||
                System.Net.IPAddress.IsLoopback(ip) ||
                ip.Equals(System.Net.IPAddress.IPv6Any) ||
                ip.Equals(System.Net.IPAddress.IPv6None)
            );
        }

        return false;
    }
}
