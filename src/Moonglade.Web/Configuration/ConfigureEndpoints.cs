using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Moonglade.Web.Configuration;

public class ConfigureEndpoints
{
    public static Task WriteResponse(HttpContext context, HealthReport result)
    {
        var obj = new
        {
            Helper.AppVersion,
            DotNetVersion = Environment.Version.ToString(),
            EnvironmentTags = Helper.GetEnvironmentTags(),
            GeoMatch = context.Request.Headers["x-afd-geo-match"]
        };

        return context.Response.WriteAsJsonAsync(obj);
    }
}