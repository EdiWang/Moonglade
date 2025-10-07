using Edi.Captcha;

namespace Moonglade.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CaptchaController(IStatelessCaptcha captcha) : ControllerBase
{
    [HttpGet("stateless")]
    public IActionResult GetSharedKeyStatelessCaptcha()
    {
        var result = captcha.GenerateCaptcha(100, 36);

        return Ok(new
        {
            token = result.Token,
            imageBase64 = Convert.ToBase64String(result.ImageBytes)
        });
    }
}
