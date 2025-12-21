using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Moonglade.Web.Extensions;

public static class WebApplicationBuilderExtension
{
    private const int MaxValueLength = 40;
    private const int KeyColumnWidth = 16;
    private const string Separator = "----------------------------------------------------------";

    public static void WriteParameterTable(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var appVersion = VersionHelper.AppVersion;
        var output = new StringBuilder();

        output.AppendLine(Separator);
        output.AppendLine($"Moonglade {appVersion} | .NET {Environment.Version}");
        output.AppendLine(Separator);

        var (ipv4Addresses, ipv6Addresses) = GetIpAddressesAsync().GetAwaiter().GetResult();
        var parameters = BuildParameterDictionary(builder.Configuration, ipv4Addresses, ipv6Addresses);

        foreach (var (key, value) in parameters)
        {
            var truncatedValue = TruncateValue(value, MaxValueLength);
            output.AppendLine($"{key,-KeyColumnWidth} | {truncatedValue,-MaxValueLength}");
        }

        output.AppendLine(Separator);

        Console.Write(output.ToString());
    }

    private static Dictionary<string, string> BuildParameterDictionary(
        IConfiguration configuration,
        string[] ipv4Addresses,
        string[] ipv6Addresses)
    {
        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "N/A";

        return new Dictionary<string, string>
        {
            { "Path", Environment.CurrentDirectory },
            { "System", VersionHelper.TryGetFullOSVersion() },
            { "User", Environment.UserName },
            { "Host", Environment.MachineName },
            { "IPv4", FormatAddresses(ipv4Addresses) },
            { "IPv6", FormatAddresses(ipv6Addresses) },
            { "URLs", configuration["Urls"] ?? "N/A" },
            { "Database", GetConnectionStringProvider(configuration) },
            { "Image storage", configuration["ImageStorage:Provider"] ?? "N/A" },
            { "Authentication", configuration["Authentication:Provider"] ?? "N/A" },
            { "Editor", configuration["Post:Editor"] ?? "N/A" },
            { "Environment", envName }
        };
    }

    private static string GetConnectionStringProvider(IConfiguration configuration)
    {
        var provider = configuration.GetConnectionString("DatabaseProvider");
        if (string.IsNullOrEmpty(provider))
        {
            // Fallback to check if there's a default connection string
            var defaultConnection = configuration.GetConnectionString("DefaultConnection");
            return string.IsNullOrEmpty(defaultConnection) ? "N/A" : "Configured";
        }
        return provider;
    }

    private static string FormatAddresses(string[] addresses)
    {
        return addresses.Length == 0 ? "N/A" : string.Join(", ", addresses);
    }

    private static string TruncateValue(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value ?? "N/A";

        return value[..(maxLength - 3)] + "...";
    }

    private static async Task<(string[] ipv4Addresses, string[] ipv6Addresses)> GetIpAddressesAsync()
    {
        try
        {
            // Use NetworkInterface for better performance and more accurate results
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                            ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            var ipv4List = new List<string>();
            var ipv6List = new List<string>();

            foreach (var networkInterface in networkInterfaces)
            {
                var ipProperties = networkInterface.GetIPProperties();
                foreach (var address in ipProperties.UnicastAddresses)
                {
                    if (address.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipv4List.Add(address.Address.ToString());
                    }
                    else if (address.Address.AddressFamily == AddressFamily.InterNetworkV6 &&
                            !address.Address.IsIPv6LinkLocal &&
                            !address.Address.IsIPv6SiteLocal)
                    {
                        ipv6List.Add(address.Address.ToString());
                    }
                }
            }

            // Fallback to DNS method if no addresses found
            if (ipv4List.Count == 0 && ipv6List.Count == 0)
            {
                return await GetIpAddressesViaDnsAsync();
            }

            return (ipv4List.ToArray(), ipv6List.ToArray());
        }
        catch (Exception ex)
        {
            // Log the exception if logging is available
            Console.WriteLine($"Warning: Failed to get IP addresses via NetworkInterface: {ex.Message}");

            // Fallback to DNS method
            return await GetIpAddressesViaDnsAsync();
        }
    }

    private static async Task<(string[] ipv4Addresses, string[] ipv6Addresses)> GetIpAddressesViaDnsAsync()
    {
        try
        {
            var hostName = Dns.GetHostName();
            var hostEntry = await Dns.GetHostEntryAsync(hostName);

            var ipv4Addresses = hostEntry.AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .Select(ip => ip.ToString())
                .ToArray();

            var ipv6Addresses = hostEntry.AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetworkV6)
                .Select(ip => ip.ToString())
                .ToArray();

            return (ipv4Addresses, ipv6Addresses);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to get IP addresses: {ex.Message}");
            return (Array.Empty<string>(), Array.Empty<string>());
        }
    }
}
