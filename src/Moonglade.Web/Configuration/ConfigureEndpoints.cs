using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moonglade.Utils;

namespace Moonglade.Web.Configuration
{
    [ExcludeFromCodeCoverage]
    public class ConfigureEndpoints
    {
        public static Action<IEndpointRouteBuilder> BlogEndpoints => endpoints =>
        {
            endpoints.MapGet("/ping", async context =>
            {
                var obj = new
                {
                    Helper.AppVersion,
                    DotNetVersion = Environment.Version.ToString(),
                    EnvironmentTags = Helper.GetEnvironmentTags(),
                    GeoMatch = context.Request.Headers["geo-match"],
                    RequestIpAddress = context.Connection.RemoteIpAddress?.ToString()
                };

                await context.Response.WriteAsJsonAsync(obj);
            });

            endpoints.MapControllers();
            endpoints.MapRazorPages();
        };
    }
}
