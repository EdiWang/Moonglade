﻿namespace Moonglade.Web.Middleware;

public class DNTMiddleware
{
    private readonly RequestDelegate _next;

    public DNTMiddleware(RequestDelegate next) => _next = next;

    public Task Invoke(HttpContext httpContext)
    {
        var dntFlag = httpContext.Request.Headers["DNT"];
        var dnt = !string.IsNullOrWhiteSpace(dntFlag) && dntFlag == "1";

        httpContext.Items["DNT"] = dnt;

        return _next.Invoke(httpContext);
    }
}