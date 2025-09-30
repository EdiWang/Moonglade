using Edi.Captcha;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Moonglade.Web.Filters;

public class ValidateCaptcha(ISessionBasedCaptcha captcha) : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var captchableModel = context.ActionArguments
            .Values
            .OfType<ICaptchable>()
            .FirstOrDefault();

        if (captchableModel == null)
        {
            AddModelError(context, "Captcha Code is required");
            context.Result = new BadRequestObjectResult(context.ModelState);
            return;
        }

        if (!captcha.Validate(captchableModel.CaptchaCode, context.HttpContext.Session))
        {
            AddModelError(context, "Wrong Captcha Code");
            context.Result = new ConflictObjectResult(context.ModelState);
            return;
        }

        base.OnActionExecuting(context);
    }

    private static void AddModelError(ActionExecutingContext context, string errorMessage)
    {
        context.ModelState.AddModelError(nameof(ICaptchable.CaptchaCode), errorMessage);
    }
}