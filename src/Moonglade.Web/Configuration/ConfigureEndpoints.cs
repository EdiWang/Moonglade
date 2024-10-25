using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Moonglade.Web.Configuration;

public class ConfigureEndpoints
{
    public static Task WriteResponse(HttpContext context, HealthReport result)
    {
        var obj = new
        {
            Helper.AppVersion,
            EnvironmentTags = Helper.GetEnvironmentTags(),
            GeoMatch = context.Request.Headers["x-geo-match"]
        };

        return context.Response.WriteAsJsonAsync(obj);
    }
}