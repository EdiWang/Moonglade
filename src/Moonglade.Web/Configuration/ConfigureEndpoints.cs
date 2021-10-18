using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moonglade.Utils;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Web.Configuration;

[ExcludeFromCodeCoverage]
public class ConfigureEndpoints
{
    public static Action<IEndpointRouteBuilder> BlogEndpoints => endpoints =>
    {
        endpoints.MapHealthChecks("/ping", new()
        {
            ResponseWriter = WriteResponse
        });

        endpoints.MapControllers();
        endpoints.MapRazorPages();
    };

    private static Task WriteResponse(HttpContext context, HealthReport result)
    {
        var obj = new
        {
            Helper.AppVersion,
            DotNetVersion = Environment.Version.ToString(),
            EnvironmentTags = Helper.GetEnvironmentTags(),
            GeoMatch = context.Request.Headers["geo-match"],
            RequestIpAddress = context.Connection.RemoteIpAddress?.ToString()
        };

        return context.Response.WriteAsJsonAsync(obj);
    }
}