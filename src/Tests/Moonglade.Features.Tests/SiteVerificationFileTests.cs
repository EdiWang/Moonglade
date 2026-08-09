using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.SiteVerification;
using Moq;

namespace Moonglade.Features.Tests;

public class SiteVerificationFileTests
{
    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BlogDbContext(options);
    }

    [Fact]
    public async Task CreateSiteVerificationFileCommand_CreatesFileWithNormalizedNameAndContentType()
    {
        using var db = CreateDbContext();
        var handler = new CreateSiteVerificationFileCommandHandler(
            db,
            Mock.Of<ILogger<CreateSiteVerificationFileCommandHandler>>());

        var result = await handler.HandleAsync(
            new CreateSiteVerificationFileCommand("Google123.HTML", "verification-token", true),
            TestContext.Current.CancellationToken);

        Assert.Equal(SiteVerificationFileOperationCode.Done, result.Code);
        Assert.Equal("Google123.HTML", result.File.FileName);
        Assert.Equal("text/html; charset=utf-8", result.File.ContentType);
        Assert.True(result.File.IsEnabled);

        var entity = await db.SiteVerificationFile.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("google123.html", entity.NormalizedFileName);
    }

    [Fact]
    public async Task CreateSiteVerificationFileCommand_DuplicateNormalizedFileName_ReturnsDuplicate()
    {
        using var db = CreateDbContext();
        db.SiteVerificationFile.Add(CreateEntity("Google123.HTML", "google123.html"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateSiteVerificationFileCommandHandler(
            db,
            Mock.Of<ILogger<CreateSiteVerificationFileCommandHandler>>());

        var result = await handler.HandleAsync(
            new CreateSiteVerificationFileCommand("google123.html", "verification-token", true),
            TestContext.Current.CancellationToken);

        Assert.Equal(SiteVerificationFileOperationCode.DuplicateFileName, result.Code);
        Assert.Single(await db.SiteVerificationFile.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateSiteVerificationFileCommand_InvalidFileName_ReturnsValidationFailed()
    {
        using var db = CreateDbContext();
        var handler = new CreateSiteVerificationFileCommandHandler(
            db,
            Mock.Of<ILogger<CreateSiteVerificationFileCommandHandler>>());

        var result = await handler.HandleAsync(
            new CreateSiteVerificationFileCommand("../google.html", "verification-token", true),
            TestContext.Current.CancellationToken);

        Assert.Equal(SiteVerificationFileOperationCode.ValidationFailed, result.Code);
        Assert.Empty(await db.SiteVerificationFile.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateSiteVerificationFileCommand_ExistingFile_UpdatesContentAndName()
    {
        using var db = CreateDbContext();
        var entity = CreateEntity("old.txt", "old.txt");
        db.SiteVerificationFile.Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateSiteVerificationFileCommandHandler(
            db,
            Mock.Of<ILogger<UpdateSiteVerificationFileCommandHandler>>());

        var result = await handler.HandleAsync(
            new UpdateSiteVerificationFileCommand(entity.Id, "new.json", "{\"ok\":true}", false),
            TestContext.Current.CancellationToken);

        Assert.Equal(SiteVerificationFileOperationCode.Done, result.Code);
        Assert.Equal("new.json", result.File.FileName);
        Assert.Equal("application/json; charset=utf-8", result.File.ContentType);
        Assert.False(result.File.IsEnabled);

        var saved = await db.SiteVerificationFile.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("new.json", saved.NormalizedFileName);
        Assert.True(saved.LastModifiedTimeUtc >= entity.CreatedTimeUtc);
    }

    [Fact]
    public async Task ToggleSiteVerificationFileCommand_ExistingFile_UpdatesEnabledState()
    {
        using var db = CreateDbContext();
        var entity = CreateEntity("google.txt", "google.txt");
        db.SiteVerificationFile.Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ToggleSiteVerificationFileCommandHandler(
            db,
            Mock.Of<ILogger<ToggleSiteVerificationFileCommandHandler>>());

        var result = await handler.HandleAsync(
            new ToggleSiteVerificationFileCommand(entity.Id, false),
            TestContext.Current.CancellationToken);

        Assert.Equal(OperationCode.Done, result);
        Assert.False((await db.SiteVerificationFile.SingleAsync(TestContext.Current.CancellationToken)).IsEnabled);
    }

    [Fact]
    public async Task GetPublicSiteVerificationFileQuery_EnabledFile_ReturnsPublicFile()
    {
        using var db = CreateDbContext();
        db.SiteVerificationFile.Add(CreateEntity("Google123.html", "google123.html"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetPublicSiteVerificationFileQueryHandler(db);

        var result = await handler.HandleAsync(
            new GetPublicSiteVerificationFileQuery("google123.html"),
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("Google123.html", result.FileName);
        Assert.Equal("verification-token", result.Content);
        Assert.StartsWith("\"", result.EntityTag, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetPublicSiteVerificationFileQuery_DisabledFile_ReturnsNull()
    {
        using var db = CreateDbContext();
        var entity = CreateEntity("google.txt", "google.txt");
        entity.IsEnabled = false;
        db.SiteVerificationFile.Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetPublicSiteVerificationFileQueryHandler(db);

        var result = await handler.HandleAsync(
            new GetPublicSiteVerificationFileQuery("google.txt"),
            TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteSiteVerificationFileCommand_ExistingFile_RemovesFile()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new BlogDbContext(options);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var entity = CreateEntity("google.txt", "google.txt");
        db.SiteVerificationFile.Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteSiteVerificationFileCommandHandler(
            db,
            Mock.Of<ILogger<DeleteSiteVerificationFileCommandHandler>>());

        var result = await handler.HandleAsync(
            new DeleteSiteVerificationFileCommand(entity.Id),
            TestContext.Current.CancellationToken);

        Assert.Equal(OperationCode.Done, result);
        Assert.Empty(await db.SiteVerificationFile.ToListAsync(TestContext.Current.CancellationToken));
    }

    private static SiteVerificationFileEntity CreateEntity(string fileName, string normalizedFileName)
    {
        var now = DateTime.UtcNow.AddMinutes(-1);
        return new SiteVerificationFileEntity
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            NormalizedFileName = normalizedFileName,
            Content = "verification-token",
            ContentType = SiteVerificationFileConstants.GetContentType(fileName),
            IsEnabled = true,
            CreatedTimeUtc = now,
            LastModifiedTimeUtc = now
        };
    }
}
