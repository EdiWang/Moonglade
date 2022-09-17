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
    public async Task<IActionResult> ExportDownload(ExportType type, CancellationToken cancellationToken)
    {
        var exportResult = type switch
        {
            ExportType.Tags => await _mediator.Send(new ExportTagsDataCommand(), cancellationToken),
            ExportType.Categories => await _mediator.Send(new ExportCategoryDataCommand(), cancellationToken),
            ExportType.FriendLinks => await _mediator.Send(new ExportLinkDataCommand(), cancellationToken),
            ExportType.Pages => await _mediator.Send(new ExportPageDataCommand(), cancellationToken),
            ExportType.Posts => await _mediator.Send(new ExportPostDataCommand(), cancellationToken),
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