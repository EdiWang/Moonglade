using Microsoft.ApplicationInsights.DataContracts;

namespace Moonglade.Web.Middleware
{
    public class ResponseBodyLoggingMiddleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var originalBodyStream = context.Response.Body;

            try
            {
                await using var memoryStream = new MemoryStream();
                context.Response.Body = memoryStream;

                await next(context);

                memoryStream.Position = 0;
                var reader = new StreamReader(memoryStream);
                var responseBody = await reader.ReadToEndAsync();

                memoryStream.Position = 0;
                await memoryStream.CopyToAsync(originalBodyStream);

                if (context.Response.StatusCode != 404 &&
                    context.Response.StatusCode is >= 400 and <= 599)
                {
                    var requestTelemetry = context.Features.Get<RequestTelemetry>();
                    requestTelemetry?.Properties.Add("ResponseBody", responseBody);
                }
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }
    }
}
