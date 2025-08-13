using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

namespace Moonglade.Web;

public static class WebApplicationExtensions
{
    private const int MaxHeaderNameLength = 40;
    private const string ForwardedHeadersSection = "ForwardedHeaders";
    private const string HeaderNameKey = "HeaderName";
    private const string KnownProxiesKey = "KnownProxies";

    // ASP.NET Core always use the last value in XFF header, which is AFD's IP address
    // Need to set as `X-Azure-ClientIP` as workaround
    // https://learn.microsoft.com/en-us/azure/frontdoor/front-door-http-headers-protocol
    public static void UseSmartXFFHeader(this WebApplication app)
    {
        var fho = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };

        ConfigureForwardedForHeaderName(app, fho);
        ConfigureKnownProxies(app, fho);

        app.UseForwardedHeaders(fho);
    }

    private static void ConfigureForwardedForHeaderName(WebApplication app, ForwardedHeadersOptions fho)
    {
        var headerName = app.Configuration[$"{ForwardedHeadersSection}:{HeaderNameKey}"];
        if (!string.IsNullOrWhiteSpace(headerName))
        {
            if (headerName.Length > MaxHeaderNameLength || !IsValidHeaderName(headerName))
            {
                app.Logger.LogWarning($"XFF header name '{headerName}' is invalid, it will not be applied");
            }
            else
            {
                fho.ForwardedForHeaderName = headerName;
            }
        }
    }

    private static void ConfigureKnownProxies(WebApplication app, ForwardedHeadersOptions fho)
    {
        var knownProxies = app.Configuration.GetSection($"{ForwardedHeadersSection}:{KnownProxiesKey}").Get<string[]>();
        if (knownProxies is { Length: > 0 })
        {
            // Fix docker deployments on Azure App Service blows up with Entra ID authentication
            // https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-6.0
            // "Outside of using IIS Integration when hosting out-of-process, Forwarded Headers Middleware isn't enabled by default."
            if (EnvironmentHelper.IsRunningInDocker())
            {
                // Fix #712
                // Adding KnownProxies will make Azure App Service boom boom with Entra ID redirect URL
                // Result in `https` incorrectly written into `http` and make `/signin-oidc` url invalid.
                app.Logger.LogWarning("Running in Docker, skip adding 'KnownProxies'.");
            }
            else
            {
                fho.ForwardLimit = null;
                fho.KnownProxies.Clear();

                foreach (var ip in knownProxies)
                {
                    if (IPAddress.TryParse(ip, out var ipAddress))
                    {
                        fho.KnownProxies.Add(ipAddress);
                    }
                    else
                    {
                        app.Logger.LogWarning($"Invalid IP address '{ip}' in KnownProxies configuration.");
                    }
                }

                app.Logger.LogInformation("Added known proxies ({0}): {1}",
                    knownProxies.Length,
                    System.Text.Json.JsonSerializer.Serialize(knownProxies));
            }
        }
        else
        {
            // Fix deployment on AFD would not get the correct client IP address because it doesn't trust network other than localhost by default
            // Add this can make ASP.NET Core read forward headers from any network with a potential security issue
            // Attackers can hide their IP by sending a fake header
            // This is OK because Moonglade is just a blog, nothing to hack, let it be
            fho.KnownNetworks.Add(new(IPAddress.Any, 0));
            fho.KnownNetworks.Add(new(IPAddress.IPv6Any, 0));
        }
    }

    private static bool IsValidHeaderName(string headerName)
    {
        if (string.IsNullOrEmpty(headerName))
        {
            return false;
        }

        // Check if header name conforms to the standard which allows:
        // - Any ASCII character from 'a' to 'z' and 'A' to 'Z'
        // - Digits from '0' to '9'
        // - Special characters: '!', '#', '$', '%', '&', ''', '*', '+', '-', '.', '^', '_', '`', '|', '~'
        return headerName.All(c =>
            c is >= 'a' and <= 'z' ||
            c is >= 'A' and <= 'Z' ||
            c is >= '0' and <= '9' ||
            c == '!' || c == '#' || c == '$' || c == '%' || c == '&' || c == '\'' ||
            c == '*' || c == '+' || c == '-' || c == '.' || c == '^' || c == '_' ||
            c == '`' || c == '|' || c == '~');
    }
}