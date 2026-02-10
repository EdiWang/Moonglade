using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Moonglade.Web.Filters;

/// <summary>
/// A global result filter that converts non-ProblemDetails error responses (4xx/5xx)
/// into RFC 7807/9457 Problem Details format. This allows controllers to keep using
/// semantic helpers like NotFound("message"), Conflict("message"), BadRequest("message")
/// while ensuring all error responses are consistently formatted.
/// </summary>
public class ProblemDetailsResultFilter(ProblemDetailsFactory problemDetailsFactory) : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is ObjectResult { StatusCode: >= 400 } objectResult
            && objectResult.Value is not ProblemDetails)
        {
            var statusCode = objectResult.StatusCode!.Value;
            var detail = objectResult.Value as string;

            var problemDetails = problemDetailsFactory.CreateProblemDetails(
                context.HttpContext,
                statusCode: statusCode,
                detail: detail,
                instance: context.HttpContext.Request.Path);

            problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

            objectResult.Value = problemDetails;
            objectResult.ContentTypes.Clear();
            objectResult.ContentTypes.Add("application/problem+json");
        }
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}
