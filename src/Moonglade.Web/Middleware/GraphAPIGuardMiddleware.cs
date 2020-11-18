using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.FeatureManagement;
using Moonglade.Model.Settings;

namespace Moonglade.Web.Middleware
{
    public class GraphAPIGuardMiddleware
    {
        private readonly RequestDelegate _next;

        public GraphAPIGuardMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IFeatureManager featureManager)
        {
            var flag = await featureManager.IsEnabledAsync(nameof(FeatureFlags.EnableWebApi));
            if (context.Request.Path.StartsWithSegments("/api/graph") && !flag)
            {
                context.Response.StatusCode = StatusCodes.Status501NotImplemented;
                await context.Response.WriteAsync("API is disabled", Encoding.UTF8);
            }
            else
            {
                await _next.Invoke(context);
            }
        }
    }
}
