using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.Features.SiteVerification;
using Moonglade.Web.Handlers;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Route("api/site-verification-files")]
public class SiteVerificationFilesController(
    ICacheAside cache,
    IQueryMediator queryMediator,
    ICommandMediator commandMediator) : BlogControllerBase(commandMediator)
{
    [HttpGet("list")]
    public async Task<IActionResult> List()
    {
        var files = await queryMediator.QueryAsync(new ListSiteVerificationFilesQuery());
        return Ok(files);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get([NotEmpty] Guid id)
    {
        var file = await queryMediator.QueryAsync(new GetSiteVerificationFileQuery(id));
        if (file == null) return NotFound();

        return Ok(file);
    }

    [HttpPost]
    public async Task<IActionResult> Create(SaveSiteVerificationFileRequest request)
    {
        var result = await CommandMediator.SendAsync(new CreateSiteVerificationFileCommand(
            request.FileName,
            request.Content,
            request.IsEnabled));

        if (result.Code == SiteVerificationFileOperationCode.ValidationFailed)
        {
            return ValidationProblem(result.ErrorMessage);
        }

        if (result.Code == SiteVerificationFileOperationCode.DuplicateFileName)
        {
            return Conflict(result.ErrorMessage);
        }

        RemoveVerificationFileCache(result.File.FileName);

        await LogActivityAsync(
            EventType.SiteVerificationFileCreated,
            "Create Site Verification File",
            result.File.FileName,
            ActivityLogMetaData.Create(
                ("FileName", result.File.FileName),
                ("ContentType", result.File.ContentType),
                ("ContentBytes", result.File.ContentBytes),
                ("IsEnabled", result.File.IsEnabled)));

        return Created($"/{result.File.FileName}", result.File);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([NotEmpty] Guid id, SaveSiteVerificationFileRequest request)
    {
        var existingFile = await queryMediator.QueryAsync(new GetSiteVerificationFileQuery(id));
        if (existingFile == null) return NotFound();

        var result = await CommandMediator.SendAsync(new UpdateSiteVerificationFileCommand(
            id,
            request.FileName,
            request.Content,
            request.IsEnabled));

        if (result.Code == SiteVerificationFileOperationCode.ValidationFailed)
        {
            return ValidationProblem(result.ErrorMessage);
        }

        if (result.Code == SiteVerificationFileOperationCode.DuplicateFileName)
        {
            return Conflict(result.ErrorMessage);
        }

        RemoveVerificationFileCache(existingFile.FileName);
        RemoveVerificationFileCache(result.File.FileName);

        await LogActivityAsync(
            EventType.SiteVerificationFileUpdated,
            "Update Site Verification File",
            result.File.FileName,
            ActivityLogMetaData.Create(
                ("FileId", id),
                ("OldFileName", existingFile.FileName),
                ("FileName", result.File.FileName),
                ("ContentType", result.File.ContentType),
                ("ContentBytes", result.File.ContentBytes),
                ("IsEnabled", result.File.IsEnabled)));

        return Ok(result.File);
    }

    [HttpPost("{id:guid}/toggle")]
    public async Task<IActionResult> Toggle([NotEmpty] Guid id, ToggleSiteVerificationFileRequest request)
    {
        var existingFile = await queryMediator.QueryAsync(new GetSiteVerificationFileQuery(id));
        if (existingFile == null) return NotFound();

        var oc = await CommandMediator.SendAsync(new ToggleSiteVerificationFileCommand(id, request.IsEnabled));
        if (oc == OperationCode.ObjectNotFound) return NotFound();

        RemoveVerificationFileCache(existingFile.FileName);

        await LogActivityAsync(
            EventType.SiteVerificationFileToggled,
            "Toggle Site Verification File",
            existingFile.FileName,
            ActivityLogMetaData.Create(
                ("FileId", id),
                ("FileName", existingFile.FileName),
                ("IsEnabled", request.IsEnabled)));

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([NotEmpty] Guid id)
    {
        var existingFile = await queryMediator.QueryAsync(new GetSiteVerificationFileQuery(id));
        if (existingFile == null) return NotFound();

        var oc = await CommandMediator.SendAsync(new DeleteSiteVerificationFileCommand(id));
        if (oc == OperationCode.ObjectNotFound) return NotFound();

        RemoveVerificationFileCache(existingFile.FileName);

        await LogActivityAsync(
            EventType.SiteVerificationFileDeleted,
            "Delete Site Verification File",
            existingFile.FileName,
            ActivityLogMetaData.Create(
                ("FileId", id),
                ("FileName", existingFile.FileName),
                ("ContentType", existingFile.ContentType),
                ("ContentBytes", existingFile.ContentBytes)));

        return NoContent();
    }

    private void RemoveVerificationFileCache(string fileName) =>
        cache.Remove(BlogCachePartition.General.ToString(), SiteVerificationFileMapHandler.GetCacheKey(fileName));
}

public sealed record SaveSiteVerificationFileRequest(
    [Required]
    [MaxLength(SiteVerificationFileConstants.MaxFileNameLength)]
    string FileName,

    [Required]
    string Content,

    bool IsEnabled);

public sealed record ToggleSiteVerificationFileRequest(bool IsEnabled);
