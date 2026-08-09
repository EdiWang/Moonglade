namespace Moonglade.Features.SiteVerification;

public sealed record SiteVerificationFileSummary(
    Guid Id,
    string FileName,
    string ContentType,
    int ContentBytes,
    bool IsEnabled,
    DateTime CreatedTimeUtc,
    DateTime LastModifiedTimeUtc);

public sealed record SiteVerificationFileDetail(
    Guid Id,
    string FileName,
    string Content,
    string ContentType,
    int ContentBytes,
    bool IsEnabled,
    DateTime CreatedTimeUtc,
    DateTime LastModifiedTimeUtc);

public sealed record PublicSiteVerificationFile(
    string FileName,
    string Content,
    string ContentType,
    DateTime LastModifiedTimeUtc,
    string EntityTag);

public enum SiteVerificationFileOperationCode
{
    None,
    Done,
    ObjectNotFound,
    DuplicateFileName,
    ValidationFailed
}

public sealed record SiteVerificationFileCommandResult(
    SiteVerificationFileOperationCode Code,
    SiteVerificationFileDetail File,
    string ErrorMessage)
{
    public static SiteVerificationFileCommandResult Done(SiteVerificationFileEntity entity) =>
        new(SiteVerificationFileOperationCode.Done, SiteVerificationFileMapper.ToDetail(entity), null);

    public static SiteVerificationFileCommandResult DuplicateFileName() =>
        new(SiteVerificationFileOperationCode.DuplicateFileName, null, "A site verification file with the same name already exists.");

    public static SiteVerificationFileCommandResult ObjectNotFound() =>
        new(SiteVerificationFileOperationCode.ObjectNotFound, null, "Site verification file was not found.");

    public static SiteVerificationFileCommandResult ValidationFailed(string errorMessage) =>
        new(SiteVerificationFileOperationCode.ValidationFailed, null, errorMessage);
}

internal static class SiteVerificationFileMapper
{
    public static SiteVerificationFileSummary ToSummary(SiteVerificationFileEntity entity) =>
        new(
            entity.Id,
            entity.FileName,
            entity.ContentType,
            Encoding.UTF8.GetByteCount(entity.Content),
            entity.IsEnabled,
            entity.CreatedTimeUtc,
            entity.LastModifiedTimeUtc);

    public static SiteVerificationFileDetail ToDetail(SiteVerificationFileEntity entity) =>
        new(
            entity.Id,
            entity.FileName,
            entity.Content,
            entity.ContentType,
            Encoding.UTF8.GetByteCount(entity.Content),
            entity.IsEnabled,
            entity.CreatedTimeUtc,
            entity.LastModifiedTimeUtc);

    public static PublicSiteVerificationFile ToPublicFile(SiteVerificationFileEntity entity) =>
        new(
            entity.FileName,
            entity.Content,
            entity.ContentType,
            entity.LastModifiedTimeUtc,
            CreateEntityTag(entity));

    private static string CreateEntityTag(SiteVerificationFileEntity entity)
    {
        var contentBytes = Encoding.UTF8.GetByteCount(entity.Content);
        return $"\"{entity.LastModifiedTimeUtc.Ticks:x}-{contentBytes:x}\"";
    }
}
