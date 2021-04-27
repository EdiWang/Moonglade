using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Auth;
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

            endpoints.MapGet("/admin", async context =>
            {
                await context.Response.CompleteAsync();
                context.Response.Redirect("/admin/post", false);
            }).RequireAuthorization();

            endpoints.MapControllers();
            endpoints.MapRazorPages();
        };
    }
}
