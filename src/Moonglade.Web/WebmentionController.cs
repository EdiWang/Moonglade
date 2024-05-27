using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web;

[ApiController]
[Route("[controller]")]
public class WebmentionController(
    ILogger<WebmentionController> logger,
    IBlogConfig blogConfig,
    IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ReceiveWebmention(
        [FromForm][Required] string source,
        [FromForm][Required] string target)
    {
        // Verify that the source URL links to the target URL
        // TODO

        // Process the Webmention
        // TODO

        // For demonstration purposes, we'll just return a success message.
        return Ok("Webmention received and verified.");
    }
}