using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Moonglade.Web.Middleware.DNT
{
    public class DNTMiddleware
    {
        private readonly RequestDelegate _next;

        public DNTMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext httpContext)
        {
            var dntFlag = httpContext.Request.Headers["DNT"];
            bool dnt = !string.IsNullOrWhiteSpace(dntFlag) && dntFlag == 1.ToString();

            httpContext.Items["DNT"] = dnt;

            return _next.Invoke(httpContext);
        }
    }
}
