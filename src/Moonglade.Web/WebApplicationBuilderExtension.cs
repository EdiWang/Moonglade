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

        var strHostName = Dns.GetHostName();
        var ipEntry = Dns.GetHostEntry(strHostName);
        var ips = ipEntry.AddressList;

        // get all IPv4 addresses
        var ipv4s = ips.Where(p => p.AddressFamily == AddressFamily.InterNetwork).ToArray();

        // get all IPv6 addresses
        var ipv6s = ips.Where(p => p.AddressFamily == AddressFamily.InterNetworkV6).ToArray();

        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        var dic = new Dictionary<string, string>
        {
            { "Path", Environment.CurrentDirectory },
            { "System", Helper.TryGetFullOSVersion() },
            { "User", Environment.UserName },
            { "Host", Environment.MachineName },
            { "IPv4", string.Join(", ", ipv4s.Select(p => p.ToString())) },
            { "IPv6", string.Join(", ", ipv6s.Select(p => p.ToString())) },
            { "URLs", builder.Configuration["Urls"]! },
            { "Database", builder.Configuration.GetConnectionString("DatabaseType")! },
            { "Image storage", builder.Configuration["ImageStorage:Provider"]! },
            { "Authentication", builder.Configuration["Authentication:Provider"]! },
            { "Editor", builder.Configuration["Editor"]! },
            { "Environment", envName ?? "N/A" }
        };

        if (!string.IsNullOrWhiteSpace(envName) && envName.ToLower() == "development")
        {
            dic.Add("Connection String", builder.Configuration.GetConnectionString("MoongladeDatabase")!);
        }

        foreach (var item in dic)
        {
            Console.WriteLine($"{item.Key,-20} | {item.Value,-35}");
        }

        Console.WriteLine("----------------------------------------------------------");
        Console.WriteLine("https://github.com/EdiWang/Moonglade");
    }
}