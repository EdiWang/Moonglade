using Microsoft.AspNetCore.Mvc.Filters;

namespace Moonglade.Pingback.Mvc
{
    public class AddPingbackHeaderAttribute : ResultFilterAttribute
    {
        private readonly string _pingbackEndpoint;

        public AddPingbackHeaderAttribute(string pingbackEndpoint)
        {
            _pingbackEndpoint = pingbackEndpoint;
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            if (!context.HttpContext.Response.Headers.ContainsKey("x-pingback"))
            {
                context.HttpContext.Response.Headers.Add("x-pingback",
                    new[]
                    {
                        $"{context.HttpContext.Request.Scheme}://{context.HttpContext.Request.Host}/{_pingbackEndpoint}"
                    });
            }

            base.OnResultExecuting(context);
        }
    }
}
