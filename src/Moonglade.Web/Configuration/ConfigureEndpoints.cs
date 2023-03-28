using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Moonglade.Web.Configuration;


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
        // debug
        var xff = context.Request.Headers["X-Forwarded-For"];

        var obj = new
        {
            Helper.AppVersion,
            DotNetVersion = Environment.Version.ToString(),
            EnvironmentTags = Helper.GetEnvironmentTags(),
            GeoMatch = context.Request.Headers["geo-match"],
            RequestIpAddress = context.Connection.RemoteIpAddress?.ToString(),
            XFF = xff
        };

        return context.Response.WriteAsJsonAsync(obj);
    }
}