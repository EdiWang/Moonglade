using Moonglade.Data.Exporting;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DataPortingController(IMediator mediator) : ControllerBase
{
    [HttpGet("export/{type}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportDownload(ExportType type, CancellationToken ct)
    {
        var exportResult = type switch
        {
            ExportType.Pages => await mediator.Send(new ExportPageDataCommand(), ct),
            ExportType.Posts => await mediator.Send(new ExportPostDataCommand(), ct),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        return PhysicalFile(exportResult.FilePath, "application/zip", Path.GetFileName(exportResult.FilePath));
    }
}