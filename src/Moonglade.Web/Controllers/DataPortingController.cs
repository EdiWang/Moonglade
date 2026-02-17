using LiteBus.Commands.Abstractions;
using Moonglade.Data.Exporting;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DataPortingController(ICommandMediator commandMediator) : ControllerBase
{
    [HttpGet("export/{type}")]
    public async Task<IActionResult> ExportDownload(ExportType type, CancellationToken ct)
    {
        var exportResult = type switch
        {
            ExportType.Pages => await commandMediator.SendAsync(new ExportPageDataCommand(), ct),
            ExportType.Posts => await commandMediator.SendAsync(new ExportPostDataCommand(), ct),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        return PhysicalFile(exportResult.FilePath, "application/zip", Path.GetFileName(exportResult.FilePath));
    }
}