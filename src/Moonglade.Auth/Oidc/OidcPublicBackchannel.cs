#nullable enable

using System.Net;
using System.Net.Sockets;

namespace Moonglade.Auth.Oidc;

internal sealed class OidcPublicBackchannel : DelegatingHandler
{
    public OidcPublicBackchannel() : base(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseProxy = false,
        UseCookies = false,
        ConnectTimeout = TimeSpan.FromSeconds(10),
        ConnectCallback = ConnectAsync
    })
    {
    }

    public static bool IsHttpsUrl(string? value) =>
        TryGetHttpsUri(value, out var uri) && uri.Query.Length == 0;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!TryGetHttpsUri(request.RequestUri?.AbsoluteUri, out _))
            throw new HttpRequestException("OIDC endpoints must use public HTTPS URLs.");
        return base.SendAsync(request, cancellationToken);
    }

    internal static async Task VerifyRedirectAsync(string value, CancellationToken ct)
    {
        if (!TryGetHttpsUri(value, out var uri))
            throw new HttpRequestException("OIDC endpoints must use public HTTPS URLs.");
        await ResolveAsync(uri.IdnHost, ct);
    }

    private static bool TryGetHttpsUri(string? value, out Uri uri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var parsed) && parsed.Scheme == Uri.UriSchemeHttps &&
            parsed.UserInfo.Length == 0 && parsed.Fragment.Length == 0 && !parsed.IsLoopback &&
            (parsed.HostNameType == UriHostNameType.Dns && parsed.IdnHost.TrimEnd('.').Contains('.') ||
                IPAddress.TryParse(parsed.IdnHost, out var ip) && IsPublic(ip)))
        {
            uri = parsed;
            return true;
        }
        uri = null!;
        return false;
    }

    private static async Task<IPAddress[]> ResolveAsync(string host, CancellationToken ct)
    {
        var addresses = await Dns.GetHostAddressesAsync(host, ct);
        if (addresses.Length == 0 || addresses.Any(address => !IsPublic(address)))
            throw new HttpRequestException("OIDC endpoints must resolve only to public addresses.");
        return addresses;
    }

    private static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken ct)
    {
        var addresses = await ResolveAsync(context.DnsEndPoint.Host, ct);
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, ct);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    internal static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return (bytes[0] & 0xe0) == 0x20
                && !(bytes[0] == 0x20 && bytes[1] == 0x01 && (bytes[2] < 2 || bytes[2] == 0x0d && bytes[3] == 0xb8))
                && !(bytes[0] == 0x20 && bytes[1] == 0x02)
                && !(bytes[0] == 0x3f && bytes[1] == 0xff && bytes[2] < 0x10);
        }
        return bytes[0] is > 0 and < 224 and not (10 or 127)
            && !(bytes[0] == 100 && bytes[1] is >= 64 and <= 127)
            && !(bytes[0] == 169 && bytes[1] == 254)
            && !(bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
            && !(bytes[0] == 192 && (bytes[1] == 168 || bytes[1] == 0 && bytes[2] is 0 or 2 || bytes[1] == 88 && bytes[2] == 99))
            && !(bytes[0] == 198 && (bytes[1] is 18 or 19 || bytes[1] == 51 && bytes[2] == 100))
            && !(bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113);
    }
}
