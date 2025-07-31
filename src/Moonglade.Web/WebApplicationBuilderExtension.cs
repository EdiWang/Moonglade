using System.Net;
using System.Net.Sockets;

namespace Moonglade.Web;

public static class WebApplicationBuilderExtension
{
    public static void WriteParameterTable(this WebApplicationBuilder builder)
    {
        var appVersion = Helper.AppVersion;
        Console.WriteLine($"Moonglade {appVersion} | .NET {Environment.Version}");
        Console.WriteLine("----------------------------------------------------------");

        var (ipv4Addresses, ipv6Addresses) = GetIpAddresses();

        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "N/A";
        var configuration = builder.Configuration;

        var parameters = new Dictionary<string, string>
        {
            { "Path", Environment.CurrentDirectory },
            { "System", Helper.TryGetFullOSVersion() },
            { "User", Environment.UserName },
            { "Host", Environment.MachineName },
            { "IPv4", string.Join(", ", ipv4Addresses) },
            { "IPv6", string.Join(", ", ipv6Addresses) },
            { "URLs", configuration["Urls"] ?? "N/A" },
            { "Database", configuration.GetConnectionString("DatabaseProvider") },
            { "Image storage", configuration["ImageStorage:Provider"] ?? "N/A" },
            { "Authentication", configuration["Authentication:Provider"] ?? "N/A" },
            { "Editor", configuration["Post:Editor"] ?? "N/A" },
            { "Environment", envName }
        };

        foreach (var (key, value) in parameters)
        {
            Console.WriteLine($"{key,-20} | {value,-35}");
        }

        Console.WriteLine("----------------------------------------------------------");
        Console.WriteLine("https://github.com/EdiWang/Moonglade");
    }

    private static (string[] ipv4Addresses, string[] ipv6Addresses) GetIpAddresses()
    {
        try
        {
            var hostName = Dns.GetHostName();
            var ipAddresses = Dns.GetHostEntry(hostName).AddressList;

            var ipv4Addresses = ipAddresses
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .Select(ip => ip.ToString())
                .ToArray();

            var ipv6Addresses = ipAddresses
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetworkV6)
                .Select(ip => ip.ToString())
                .ToArray();

            return (ipv4Addresses, ipv6Addresses);
        }
        catch
        {
            return (Array.Empty<string>(), Array.Empty<string>());
        }
    }
}
