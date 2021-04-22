using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moonglade.Configuration;
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
                    EnvironmentTags = Helper.GetEnvironmentTags()
                };

                await context.Response.WriteAsJsonAsync(obj);
            });
            endpoints.MapControllerRoute(
                "default",
                "{controller=Home}/{action=Index}/{id?}");
            endpoints.MapRazorPages();
        };
    }
}
