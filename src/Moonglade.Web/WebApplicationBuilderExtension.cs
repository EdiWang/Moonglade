using System.Net;

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

        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        var dic = new Dictionary<string, string>
        {
            { "Path", Environment.CurrentDirectory },
            { "System", Helper.TryGetFullOSVersion() },
            { "User", Environment.UserName },
            { "Host", Environment.MachineName },
            { "IP", string.Join(", ", ips.Select(p => p.ToString())) },
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