using Microsoft.AspNetCore.Mvc;

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
                CombinedErrorMessage = context.ModelState.CombineErrorMessages(),
                RequestId = context.HttpContext.TraceIdentifier
            };

            return new ObjectResult(errorModel)
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        };
    };
}