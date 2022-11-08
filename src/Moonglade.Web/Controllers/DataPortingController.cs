using Moonglade.Data.Exporting;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DataPortingController : ControllerBase
{
    private readonly IMediator _mediator;

    public DataPortingController(IMediator mediator) => _mediator = mediator;

    [HttpGet("export/{type}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportDownload(ExportType type, CancellationToken ct)
    {
        var exportResult = type switch
        {
            ExportType.Tags => await _mediator.Send(new ExportTagsDataCommand(), ct),
            ExportType.Categories => await _mediator.Send(new ExportCategoryDataCommand(), ct),
            ExportType.FriendLinks => await _mediator.Send(new ExportLinkDataCommand(), ct),
            ExportType.Pages => await _mediator.Send(new ExportPageDataCommand(), ct),
            ExportType.Posts => await _mediator.Send(new ExportPostDataCommand(), ct),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        switch (exportResult.ExportFormat)
        {
            case ExportFormat.SingleJsonFile:
                return new FileContentResult(exportResult.Content, exportResult.ContentType)
                {
                    FileDownloadName = $"moonglade-{type.ToString().ToLowerInvariant()}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.json"
                };

            case ExportFormat.SingleCSVFile:
                Response.Headers.Add("Content-Disposition", $"attachment;filename={Path.GetFileName(exportResult.FilePath)}");
                return PhysicalFile(exportResult.FilePath!, exportResult.ContentType, Path.GetFileName(exportResult.FilePath));

            case ExportFormat.ZippedJsonFiles:
                return PhysicalFile(exportResult.FilePath, exportResult.ContentType, Path.GetFileName(exportResult.FilePath));

            default:
                return BadRequest(ModelState.CombineErrorMessages());
        }
    }
}