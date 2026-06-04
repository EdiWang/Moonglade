using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Category;
using Moq;

namespace Moonglade.Features.Tests;

public class CategoryCommandTests
{
    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BlogDbContext(options);
    }

    [Fact]
    public async Task CreateCategoryCommand_CreatesTrimmedCategory()
    {
        using var db = CreateDbContext();
        var handler = new CreateCategoryCommandHandler(db, Mock.Of<ILogger<CreateCategoryCommandHandler>>());

        await handler.HandleAsync(new CreateCategoryCommand
        {
            DisplayName = "  .NET  ",
            Slug = "  dotnet  ",
            Note = "  Posts about .NET  "
        }, TestContext.Current.CancellationToken);

        var category = await db.Category.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(".NET", category.DisplayName);
        Assert.Equal("dotnet", category.Slug);
        Assert.Equal("Posts about .NET", category.Note);
    }

    [Fact]
    public async Task CreateCategoryCommand_DuplicateSlug_DoesNotCreateCategory()
    {
        using var db = CreateDbContext();
        db.Category.Add(CreateCategoryEntity(Guid.NewGuid(), "dotnet"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateCategoryCommandHandler(db, Mock.Of<ILogger<CreateCategoryCommandHandler>>());

        await handler.HandleAsync(new CreateCategoryCommand
        {
            DisplayName = "Duplicate",
            Slug = "dotnet",
            Note = "Duplicate slug"
        }, TestContext.Current.CancellationToken);

        Assert.Single(await db.Category.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateCategoryCommand_ExistingCategory_UpdatesFieldsAndReturnsDone()
    {
        using var db = CreateDbContext();
        var categoryId = Guid.NewGuid();
        db.Category.Add(CreateCategoryEntity(categoryId, "old"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateCategoryCommandHandler(db, Mock.Of<ILogger<UpdateCategoryCommandHandler>>());

        var result = await handler.HandleAsync(new UpdateCategoryCommand
        {
            Id = categoryId,
            DisplayName = "  New Name  ",
            Slug = "  new-slug  ",
            Note = "  New note  "
        }, TestContext.Current.CancellationToken);

        Assert.Equal(OperationCode.Done, result);
        var category = await db.Category.SingleAsync(c => c.Id == categoryId, TestContext.Current.CancellationToken);
        Assert.Equal("New Name", category.DisplayName);
        Assert.Equal("new-slug", category.Slug);
        Assert.Equal("New note", category.Note);
    }

    [Fact]
    public async Task UpdateCategoryCommand_MissingCategory_ReturnsObjectNotFound()
    {
        using var db = CreateDbContext();
        var handler = new UpdateCategoryCommandHandler(db, Mock.Of<ILogger<UpdateCategoryCommandHandler>>());

        var result = await handler.HandleAsync(new UpdateCategoryCommand
        {
            Id = Guid.NewGuid(),
            DisplayName = "Name",
            Slug = "slug",
            Note = "Note"
        }, TestContext.Current.CancellationToken);

        Assert.Equal(OperationCode.ObjectNotFound, result);
    }

    [Fact]
    public async Task DeleteCategoryCommand_ExistingCategory_RemovesCategoryAndReturnsDone()
    {
        using var db = CreateDbContext();
        var categoryId = Guid.NewGuid();
        db.Category.Add(CreateCategoryEntity(categoryId, "dotnet"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteCategoryCommandHandler(db, Mock.Of<ILogger<DeleteCategoryCommandHandler>>());

        var result = await handler.HandleAsync(new DeleteCategoryCommand(categoryId), TestContext.Current.CancellationToken);

        Assert.Equal(OperationCode.Done, result);
        Assert.Empty(await db.Category.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteCategoryCommand_MissingCategory_ReturnsObjectNotFound()
    {
        using var db = CreateDbContext();
        var handler = new DeleteCategoryCommandHandler(db, Mock.Of<ILogger<DeleteCategoryCommandHandler>>());

        var result = await handler.HandleAsync(new DeleteCategoryCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.Equal(OperationCode.ObjectNotFound, result);
    }

    private static CategoryEntity CreateCategoryEntity(Guid id, string slug)
    {
        return new CategoryEntity
        {
            Id = id,
            Slug = slug,
            DisplayName = "Category",
            Note = "Note"
        };
    }
}
