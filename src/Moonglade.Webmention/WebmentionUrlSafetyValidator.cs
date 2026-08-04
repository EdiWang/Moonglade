using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace Moonglade.Webmention;

public interface IWebmentionUrlSafetyValidator
{
    Task<bool> IsSafeSourceAsync(Uri uri, CancellationToken cancellationToken = default);
}

public interface IWebmentionDnsResolver
{
    Task<IPAddress[]> GetHostAddressesAsync(string host, CancellationToken cancellationToken);
}

public class WebmentionDnsResolver : IWebmentionDnsResolver
{
    public Task<IPAddress[]> GetHostAddressesAsync(string host, CancellationToken cancellationToken) =>
        Dns.GetHostAddressesAsync(host, cancellationToken);
}

public class WebmentionUrlSafetyValidator(IWebmentionDnsResolver dnsResolver) : IWebmentionUrlSafetyValidator
{
    public async Task<bool> IsSafeSourceAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        if (uri.Scheme is not "http" and not "https") return false;
        if (string.IsNullOrWhiteSpace(uri.Host)) return false;

        if (IPAddress.TryParse(uri.Host, out var literalAddress))
        {
            return IsPublicAddress(literalAddress);
        }

        IPAddress[] addresses;
        try
        {
            addresses = await dnsResolver.GetHostAddressesAsync(uri.IdnHost, cancellationToken);
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException)
        {
            return false;
        }

        return addresses.Length > 0 && addresses.All(IsPublicAddress);
    }

    internal static bool IsPublicAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address)) return false;
        if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.None) || address.Equals(IPAddress.Broadcast)) return false;
        if (address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.IPv6None) || address.Equals(IPAddress.IPv6Loopback)) return false;

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => IsPublicIPv4(address.GetAddressBytes()),
            AddressFamily.InterNetworkV6 => IsPublicIPv6(address.GetAddressBytes()),
            _ => false
        };
    }

    private static bool IsPublicIPv4(byte[] bytes)
    {
        if (bytes.Length != 4) return false;

        return bytes[0] switch
        {
            0 => false,                                      // 0.0.0.0/8
            10 => false,                                     // 10.0.0.0/8
            100 when bytes[1] is >= 64 and <= 127 => false,  // 100.64.0.0/10
            127 => false,                                    // 127.0.0.0/8
            169 when bytes[1] == 254 => false,               // 169.254.0.0/16
            172 when bytes[1] is >= 16 and <= 31 => false,   // 172.16.0.0/12
            192 when bytes[1] == 0 && bytes[2] == 0 => false, // 192.0.0.0/24, including 192.0.0.0/29
            192 when bytes[1] == 0 && bytes[2] == 2 => false, // 192.0.2.0/24 documentation
            192 when bytes[1] == 88 && bytes[2] == 99 => false, // 192.88.99.0/24 6to4 relay
            192 when bytes[1] == 168 => false,               // 192.168.0.0/16
            198 when bytes[1] is 18 or 19 => false,          // 198.18.0.0/15
            198 when bytes[1] == 51 && bytes[2] == 100 => false, // 198.51.100.0/24 documentation
            203 when bytes[1] == 0 && bytes[2] == 113 => false, // 203.0.113.0/24 documentation
            >= 224 => false,                                 // multicast, reserved, broadcast
            _ => true
        };
    }

    private static bool IsPublicIPv6(byte[] bytes)
    {
        if (bytes.Length != 16) return false;

        if (bytes.All(b => b == 0)) return false;                    // ::
        if (bytes[0] == 0xff) return false;                          // ff00::/8 multicast
        if ((bytes[0] & 0xfe) == 0xfc) return false;                 // fc00::/7 unique local
        if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80) return false; // fe80::/10 link-local
        if (bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8) return false; // 2001:db8::/32 documentation
        if (bytes.Take(15).All(b => b == 0) && bytes[15] == 1) return false; // ::1

        return true;
    }
}

public class WebmentionSafeHttpMessageHandlerFactory(IWebmentionDnsResolver dnsResolver)
{
    public HttpMessageHandler Create() =>
        new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            ConnectCallback = ConnectToSafeAddressAsync
        };

    private async ValueTask<Stream> ConnectToSafeAddressAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        var addresses = await dnsResolver.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
        if (addresses.Length == 0 || addresses.Any(address => !WebmentionUrlSafetyValidator.IsPublicAddress(address)))
        {
            throw new HttpRequestException($"Webmention source host '{context.DnsEndPoint.Host}' resolved to an unsafe address.");
        }

        Exception? lastException = null;
        foreach (var address in addresses)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (SocketException ex)
            {
                socket.Dispose();
                lastException = ex;
            }
        }

        throw new HttpRequestException($"Unable to connect to Webmention source host '{context.DnsEndPoint.Host}'.", lastException);
    }
}
