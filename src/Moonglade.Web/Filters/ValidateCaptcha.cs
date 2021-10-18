using Edi.Captcha;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Moonglade.Web.Filters
{
    public class ValidateCaptcha : ActionFilterAttribute
    {
        private readonly ISessionBasedCaptcha _captcha;

        public ValidateCaptcha(ISessionBasedCaptcha captcha)
        {
            _captcha = captcha;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var captchaedModel =
                context.ActionArguments.Where(p => p.Value is ICaptchable)
                                       .Select(x => x.Value as ICaptchable)
                                       .FirstOrDefault();

            if (null == captchaedModel)
            {
                context.ModelState.AddModelError(nameof(captchaedModel.CaptchaCode), "Captcha Code is required");
                context.Result = new BadRequestObjectResult(context.ModelState);
            }
            else
            {
                if (!_captcha.Validate(captchaedModel.CaptchaCode, context.HttpContext.Session))
                {
                    context.ModelState.AddModelError(nameof(captchaedModel.CaptchaCode), "Wrong Captcha Code");
                    context.Result = new ConflictObjectResult(context.ModelState);
                }
                else
                {
                    base.OnActionExecuting(context);
                }
            }
        }
    }
}
