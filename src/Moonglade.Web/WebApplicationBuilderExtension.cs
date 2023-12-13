using Spectre.Console;
using System.Net;

namespace Moonglade.Web;

public static class WebApplicationBuilderExtension
{
    public static void WriteParameterTable(this WebApplicationBuilder builder)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var appVersion = Helper.AppVersion;
        var table = new Table
        {
            Title = new($"Moonglade.Web {appVersion} | .NET {Environment.Version}")
        };

        var strHostName = Dns.GetHostName();
        var ipEntry = Dns.GetHostEntry(strHostName);
        var ips = ipEntry.AddressList;

        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        table.AddColumn("Parameter");
        table.AddColumn("Value");
        table.AddRow(new Markup("[blue]Path[/]"), new Text(Environment.CurrentDirectory));
        table.AddRow(new Markup("[blue]System[/]"), new Text(Helper.TryGetFullOSVersion()));
        table.AddRow(new Markup("[blue]User[/]"), new Text(Environment.UserName));
        table.AddRow(new Markup("[blue]Host[/]"), new Text(Environment.MachineName));
        table.AddRow(new Markup("[blue]IP addresses[/]"), new Rows(ips.Select(p => new Text(p.ToString()))));
        table.AddRow(new Markup("[blue]Database type[/]"), new Text(builder.Configuration.GetConnectionString("DatabaseType")!));

        if (!string.IsNullOrWhiteSpace(envName) && envName.ToLower() == "development")
        {
            table.AddRow(new Markup("[blue]Connection String[/]"), new Text(builder.Configuration.GetConnectionString("MoongladeDatabase")!));
        }

        table.AddRow(new Markup("[blue]Image storage[/]"), new Text(builder.Configuration["ImageStorage:Provider"]!));
        table.AddRow(new Markup("[blue]Authentication provider[/]"), new Text(builder.Configuration["Authentication:Provider"]!));
        table.AddRow(new Markup("[blue]Editor[/]"), new Text(builder.Configuration["Editor"]!));
        table.AddRow(new Markup("[blue]ASPNETCORE_ENVIRONMENT[/]"), new Text(envName ?? "N/A"));

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("[link=https://github.com/EdiWang/Moonglade]GitHub: EdiWang/Moonglade[/]");
    }
}