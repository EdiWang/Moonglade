using Moonglade.Web.Extensions;

namespace Moonglade.Web.Configuration;

public class ConfigureApiBehavior
{
    public static Action<ApiBehaviorOptions> BlogApiBehavior => options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            // Refer https://source.dot.net/#Microsoft.AspNetCore.Mvc.Core/ControllerBase.cs,1885
            var errorModel = new
            {
                Errors = context.ModelState.GetErrorMessages(),
                RequestId = context.HttpContext.TraceIdentifier
            };

            return new ObjectResult(errorModel)
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        };
    };
}