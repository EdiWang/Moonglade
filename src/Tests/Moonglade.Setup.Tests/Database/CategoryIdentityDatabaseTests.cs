using LiteBus.Events.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Category;
using Moonglade.Features.Post;
using Moq;
using System.Data.Common;

namespace Moonglade.Setup.Tests.Database;

public class CategoryIdentityDatabaseTests(DisposableCategoryDatabases databases) : IClassFixture<DisposableCategoryDatabases>
{
    private const string FirstCategory = "11111111-1111-1111-1111-111111111111";
    private const string SecondCategory = "22222222-2222-2222-2222-222222222222";
    private const string FirstPost = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    private const string SecondPost = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public async Task FreshProviderSchema_DefaultSeedAndCategoryArticleWrites_UseGeneratedIntegerIds(string provider)
    {
        await using var database = await databases.CreateAsync(provider);
        await using var db = database.Context();
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        await Seed.SeedAsync(db, Mock.Of<ILogger>());
        db.ChangeTracker.Clear();
        var category = await db.Category.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(100, category.Id);
        Assert.Equal("default", category.Slug);
        Assert.Equal("Default", category.DisplayName);
        Assert.Equal("Default Category", category.Note);
        var welcome = await db.Post.SingleAsync(TestContext.Current.CancellationToken);
        Assert.NotEqual(Guid.Empty, welcome.Id);
        Assert.Equal("welcome-to-moonglade", welcome.Slug);
        Assert.Contains("Getting Started", welcome.PostContent);
        await AssertIdentityAsync(database);

        var createCategory = new CreateCategoryCommandHandler(db, Mock.Of<ILogger<CreateCategoryCommandHandler>>());
        await createCategory.HandleAsync(new CreateCategoryCommand
        {
            DisplayName = "  Cloud  ", Slug = "cloud", Note = "  Cloud notes  "
        }, TestContext.Current.CancellationToken);
        db.ChangeTracker.Clear();
        var cloud = await db.Category.SingleAsync(c => c.Slug == "cloud", TestContext.Current.CancellationToken);
        Assert.Equal(101, cloud.Id);
        Assert.Equal("Cloud", cloud.DisplayName);
        Assert.Equal("Cloud notes", cloud.Note);
        var payload = PostPayload([100, 101]);
        var events = Mock.Of<IEventMediator>();
        var created = await new CreatePostCommandHandler(db, Mock.Of<ILogger<CreatePostCommandHandler>>(), events)
            .HandleAsync(new CreatePostCommand(payload), TestContext.Current.CancellationToken);
        db.ChangeTracker.Clear();
        var persisted = await new GetPostByIdQueryHandler(db)
            .HandleAsync(new GetPostByIdQuery(created.Id), TestContext.Current.CancellationToken);
        Assert.NotEqual(Guid.Empty, persisted.Id);
        Assert.Equal("Article content", persisted.PostContent);
        Assert.Equal([100, 101], persisted.PostCategory.Select(pc => pc.CategoryId).Order().ToArray());
        Assert.All(persisted.PostCategory, pc => Assert.Equal(created.Id, pc.PostId));
        Assert.Equal(1, await new CountPostQueryHandler(db).HandleAsync(new CountPostQuery(CountType.Category, 101),
            TestContext.Current.CancellationToken));
        Assert.Equal("article", Assert.Single(await new ListPostsQueryHandler(db)
            .HandleAsync(new ListPostsQuery(10, 1, 101), TestContext.Current.CancellationToken)).Slug);

        payload.SelectedCatIds = [101];
        payload.EditorContent = "Updated article content";
        await new UpdatePostCommandHandler(db, Mock.Of<ILogger<UpdatePostCommandHandler>>(), events)
            .HandleAsync(new UpdatePostCommand(created.Id, payload), TestContext.Current.CancellationToken);
        db.ChangeTracker.Clear();
        var updated = await new GetPostByIdQueryHandler(db)
            .HandleAsync(new GetPostByIdQuery(created.Id), TestContext.Current.CancellationToken);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal(created.RouteLink, updated.RouteLink);
        Assert.Equal("Updated article content", updated.PostContent);
        Assert.Equal(101, Assert.Single(updated.PostCategory).CategoryId);
        Assert.Equal(0, await new CountPostQueryHandler(db).HandleAsync(new CountPostQuery(CountType.Category, 100),
            TestContext.Current.CancellationToken));
        var deleted = await new DeleteCategoryCommandHandler(db, Mock.Of<ILogger<DeleteCategoryCommandHandler>>(), events)
            .HandleAsync(new DeleteCategoryCommand(101), TestContext.Current.CancellationToken);
        Assert.Equal(OperationCode.Done, deleted);
        db.ChangeTracker.Clear();
        Assert.Equal(2, await db.Post.CountAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await db.PostCategory.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(100, (await db.Category.SingleAsync(TestContext.Current.CancellationToken)).Id);
        // A manually upgraded script is also a no-op on a fresh provider-created schema.
        await database.MigrateAsync();
        await createCategory.HandleAsync(new CreateCategoryCommand { DisplayName = "Next", Slug = "next", Note = "Next" },
            TestContext.Current.CancellationToken);
        Assert.Equal(102, (await db.Category.SingleAsync(c => c.Slug == "next", TestContext.Current.CancellationToken)).Id);
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public async Task LegacyMigration_PreservesContentLinksKeysAndSequenceAcrossEditsDeletesAndReruns(string provider)
    {
        await using var db = await databases.CreateAsync(provider);
        await LegacySchemaAsync(db, populated: true);
        var content = await ContentAsync(db);
        var links = await LinksAsync(db);
        var posts = await PostsAsync(db);
        var keys = await KeysAsync(db);
        await db.MigrateAsync();
        await AssertIdentityAsync(db);
        Assert.Equal(content, await ContentAsync(db));
        Assert.Equal(links, await LinksAsync(db));
        Assert.Equal(posts, await PostsAsync(db));
        Assert.Equal(keys, await KeysAsync(db));
        Assert.Equal(["100", "101"], (await db.RowsAsync("""SELECT "Id" FROM "Category" ORDER BY "Id" """))
            .Select(row => row[0]).ToArray());
        await AssertRerunUnchangedAsync(db);

        var cloudId = (await db.RowsAsync("""SELECT "Id" FROM "Category" WHERE "Slug" = 'cloud' """))[0][0];
        var lifeId = (await db.RowsAsync("""SELECT "Id" FROM "Category" WHERE "Slug" = 'life' """))[0][0];
        Assert.NotEqual(cloudId, lifeId);
        var insertedId = await InsertCategoryAsync(db, "new");
        Assert.Equal(102, insertedId);
        await db.ExecuteAsync($$"""
            UPDATE "Category" SET "DisplayName" = 'Edited cloud', "Note" = 'Edited note' WHERE "Id" = {{cloudId}};
            DELETE FROM "Category" WHERE "Id" = {{insertedId}};
            DELETE FROM "Category" WHERE "Id" = {{lifeId}};
            """);
        Assert.Equal(["Edited cloud", "Edited note"], (await db.RowsAsync(
            """SELECT "DisplayName", "Note" FROM "Category" WHERE "Slug" = 'cloud' """))[0]);
        Assert.Equal(2, (await PostsAsync(db)).Length);
        Assert.Equal([new[] { FirstPost, "cloud" }], await LinksAsync(db));
        await AssertRerunUnchangedAsync(db);
        Assert.Equal(103, await InsertCategoryAsync(db, "after-rerun"));
        await Assert.ThrowsAnyAsync<DbException>(() => db.ExecuteAsync($$"""
            INSERT INTO "PostCategory" ("PostId", "CategoryId") VALUES ('{{SecondPost}}', 999999);
            """));
        await Assert.ThrowsAnyAsync<DbException>(() => db.ExecuteAsync($$"""
            INSERT INTO "PostCategory" ("PostId", "CategoryId") VALUES ('{{FirstPost}}', {{cloudId}});
            """));
        await db.ExecuteAsync($$"""DELETE FROM "Post" WHERE "Id" = '{{FirstPost}}';""");
        Assert.Empty(await LinksAsync(db));
        Assert.Equal(2, (await ContentAsync(db)).Length);
        Assert.Equal(SecondPost, (await PostsAsync(db))[0][0]);
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public async Task EmptyLegacyMigration_FirstInsertIs100AndRerunAfterDeleteDoesNotReuseId(string provider)
    {
        await using var db = await databases.CreateAsync(provider);
        await LegacySchemaAsync(db, populated: false);
        var posts = await PostsAsync(db);
        await db.MigrateAsync();
        await AssertIdentityAsync(db);
        await AssertRerunUnchangedAsync(db);
        Assert.Empty(await ContentAsync(db));
        Assert.Equal(posts, await PostsAsync(db));
        Assert.Equal(100, await InsertCategoryAsync(db, "first"));
        await db.ExecuteAsync("""DELETE FROM "Category" WHERE "Id" = 100;""");
        await AssertRerunUnchangedAsync(db);
        Assert.Equal(101, await InsertCategoryAsync(db, "second"));
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public async Task LegacyMigration_CustomNamesDescendingIndexAndForeignKeyActions_ArePreserved(string provider)
    {
        await using var db = await databases.CreateAsync(provider);
        await LegacySchemaAsync(db, populated: true, customNames: true);
        var keys = await KeysAsync(db);
        var links = await LinksAsync(db);
        await db.MigrateAsync();
        Assert.Equal(keys, await KeysAsync(db));
        Assert.Equal(links, await LinksAsync(db));
        await AssertIdentityAsync(db);
        await AssertRerunUnchangedAsync(db);
        var cloudId = (await db.RowsAsync("""SELECT "Id" FROM "Category" WHERE "Slug" = 'cloud' """))[0][0];
        await Assert.ThrowsAnyAsync<DbException>(() => db.ExecuteAsync($$"""DELETE FROM "Category" WHERE "Id" = {{cloudId}};"""));
        Assert.Equal(links, await LinksAsync(db));
        Assert.Equal(2, (await ContentAsync(db)).Length);
    }

    [DisposableDatabaseTheory]
    [InlineData("PostgreSql", "missing-tables")]
    [InlineData("SqlServer", "missing-tables")]
    [InlineData("PostgreSql", "missing-index")]
    [InlineData("SqlServer", "missing-index")]
    [InlineData("PostgreSql", "orphan")]
    [InlineData("SqlServer", "orphan")]
    [InlineData("PostgreSql", "extra-index")]
    [InlineData("SqlServer", "extra-index")]
    [InlineData("PostgreSql", "check")]
    [InlineData("SqlServer", "check")]
    [InlineData("PostgreSql", "extra-fk")]
    [InlineData("SqlServer", "extra-fk")]
    [InlineData("PostgreSql", "temporary-column")]
    [InlineData("SqlServer", "temporary-column")]
    [InlineData("PostgreSql", "integer-without-identity")]
    [InlineData("SqlServer", "integer-without-identity")]
    [InlineData("PostgreSql", "wrong-seed")]
    [InlineData("SqlServer", "wrong-seed")]
    [InlineData("PostgreSql", "wrong-increment")]
    [InlineData("SqlServer", "wrong-increment")]
    [InlineData("PostgreSql", "wrong-post-id")]
    [InlineData("SqlServer", "wrong-post-id")]
    [InlineData("PostgreSql", "missing-pk")]
    [InlineData("SqlServer", "missing-pk")]
    [InlineData("PostgreSql", "missing-fk")]
    [InlineData("SqlServer", "missing-fk")]
    public async Task LegacyMigration_InvalidPreconditions_RollsBackSchemaContentAndLinks(string provider, string fault)
    {
        await using var db = await databases.CreateAsync(provider);
        if (fault == "missing-tables")
        {
            var exception = await Assert.ThrowsAnyAsync<DbException>(() => db.MigrateAsync());
            Assert.Contains("requires", exception.Message);
            Assert.Empty(await ColumnsAsync(db));
            return;
        }
        if (fault is "integer-without-identity" or "wrong-seed" or "wrong-increment" or "wrong-post-id")
        {
            var postGuid = provider == "PostgreSql" ? "uuid" : "uniqueidentifier";
            var seed = fault == "wrong-seed" ? 1 : 100;
            var increment = fault == "wrong-increment" ? 2 : 1;
            var identity = fault == "integer-without-identity" ? "" : provider == "PostgreSql"
                ? $"GENERATED BY DEFAULT AS IDENTITY (START WITH {seed} INCREMENT BY {increment})"
                : $"IDENTITY({seed}, {increment})";
            var postType = fault == "wrong-post-id" ? "integer" : postGuid;
            await db.ExecuteAsync($$"""
                CREATE TABLE "Category" ("Id" integer {{identity}} NOT NULL PRIMARY KEY);
                CREATE TABLE "PostCategory" ("PostId" {{postType}} NOT NULL, "CategoryId" integer NOT NULL);
                """);
            var columns = await ColumnsAsync(db);
            var exception = await Assert.ThrowsAnyAsync<DbException>(() => db.MigrateAsync());
            Assert.Contains(fault == "wrong-post-id" ? "article IDs" : "required identity schema", exception.Message);
            Assert.Equal(columns, await ColumnsAsync(db));
            return;
        }
        await LegacySchemaAsync(db, populated: true);
        var guidType = provider == "PostgreSql" ? "uuid" : "uniqueidentifier";
        var faultSql = fault switch
        {
            "missing-index" => provider == "PostgreSql"
                ? """DROP INDEX "IX_PostCategory_CategoryId";"""
                : """DROP INDEX "IX_PostCategory_CategoryId" ON "PostCategory";""",
            "extra-index" => """CREATE INDEX "IX_Extra_CategoryId" ON "PostCategory" ("CategoryId", "PostId");""",
            "missing-pk" => """ALTER TABLE "PostCategory" DROP CONSTRAINT "PK_PostCategory";""",
            "missing-fk" => """ALTER TABLE "PostCategory" DROP CONSTRAINT "FK_PostCategory_Category";""",
            "check" => """ALTER TABLE "Category" ADD CONSTRAINT "CK_Category" CHECK ("Slug" <> '');""",
            "extra-fk" => $$"""
                CREATE TABLE "ExtraCategoryReference" ("CategoryId" {{guidType}} NOT NULL,
                    CONSTRAINT "FK_Extra_Category" FOREIGN KEY ("CategoryId") REFERENCES "Category" ("Id"));
                """,
            "temporary-column" => """ALTER TABLE "Category" ADD "IdInt" integer;""",
            "orphan" => provider == "PostgreSql"
                ? $$"""
                    ALTER TABLE "PostCategory" DROP CONSTRAINT "FK_PostCategory_Category";
                    INSERT INTO "PostCategory" ("PostId", "CategoryId")
                        VALUES ('{{SecondPost}}', '33333333-3333-3333-3333-333333333333');
                    ALTER TABLE "PostCategory" ADD CONSTRAINT "FK_PostCategory_Category"
                        FOREIGN KEY ("CategoryId") REFERENCES "Category" ("Id") ON DELETE CASCADE NOT VALID;
                    """
                : $$"""
                    ALTER TABLE "PostCategory" NOCHECK CONSTRAINT "FK_PostCategory_Category";
                    INSERT INTO "PostCategory" ("PostId", "CategoryId")
                        VALUES ('{{SecondPost}}', '33333333-3333-3333-3333-333333333333');
                    """,
            _ => throw new ArgumentOutOfRangeException(nameof(fault))
        };
        await db.ExecuteAsync(faultSql);
        var beforeColumns = await ColumnsAsync(db);
        var beforeKeys = await KeysAsync(db);
        var beforeContent = await ContentAsync(db);
        var beforePosts = await PostsAsync(db);
        var beforeLinks = await db.RowsAsync("""SELECT "PostId", "CategoryId" FROM "PostCategory" ORDER BY "PostId", "CategoryId" """);
        var error = await Assert.ThrowsAnyAsync<DbException>(() => db.MigrateAsync());
        Assert.Contains(fault switch
        {
            "orphan" => "Unmapped article category",
            "temporary-column" => "Temporary migration columns",
            "extra-fk" or "missing-fk" => "Unexpected category foreign keys",
            "missing-pk" => "Unexpected category primary keys",
            _ => "Unexpected category indexes or check constraints"
        }, error.Message);
        Assert.Equal(beforeColumns, await ColumnsAsync(db));
        Assert.Equal(beforeKeys, await KeysAsync(db));
        Assert.Equal(beforeContent, await ContentAsync(db));
        Assert.Equal(beforePosts, await PostsAsync(db));
        Assert.Equal(beforeLinks, await db.RowsAsync("""SELECT "PostId", "CategoryId" FROM "PostCategory" ORDER BY "PostId", "CategoryId" """));
    }

    private static async Task AssertRerunUnchangedAsync(CategoryTestDatabase db)
    {
        var content = await db.RowsAsync("""SELECT "Id", "Slug", "DisplayName", "Note" FROM "Category" ORDER BY "Slug" """);
        var posts = await PostsAsync(db);
        var links = await LinksAsync(db);
        var keys = await KeysAsync(db);
        var identity = await IdentityAsync(db);
        await db.MigrateAsync();
        Assert.Equal(content, await db.RowsAsync("""SELECT "Id", "Slug", "DisplayName", "Note" FROM "Category" ORDER BY "Slug" """));
        Assert.Equal(posts, await PostsAsync(db));
        Assert.Equal(links, await LinksAsync(db));
        Assert.Equal(keys, await KeysAsync(db));
        Assert.Equal(identity, await IdentityAsync(db));
    }

    private static async Task<int> InsertCategoryAsync(CategoryTestDatabase db, string slug)
    {
        var sql = db.Provider == "PostgreSql"
            ? $"""INSERT INTO "Category" ("Slug", "DisplayName", "Note") VALUES ('{slug}', 'Inserted', 'Inserted note') RETURNING "Id";"""
            : $"""INSERT INTO "Category" ("Slug", "DisplayName", "Note") OUTPUT INSERTED."Id" VALUES ('{slug}', 'Inserted', 'Inserted note');""";
        return int.Parse((await db.RowsAsync(sql))[0][0], System.Globalization.CultureInfo.InvariantCulture);
    }

    private static Task<string[][]> ContentAsync(CategoryTestDatabase db) => db.RowsAsync(
        """SELECT "Slug", "DisplayName", "Note" FROM "Category" ORDER BY "Slug" """);

    private static Task<string[][]> PostsAsync(CategoryTestDatabase db) => db.RowsAsync(
        """SELECT "Id", "Title", "Slug", "PostContent", "CreateTimeUtc", "LastModifiedUtc", "PubDateUtc", "RouteLink" FROM "Post" ORDER BY "Id" """);

    private static Task<string[][]> LinksAsync(CategoryTestDatabase db) => db.RowsAsync(
        """SELECT pc."PostId", c."Slug" FROM "PostCategory" pc JOIN "Category" c ON c."Id" = pc."CategoryId" ORDER BY pc."PostId", c."Slug" """);

    private static Task<string[][]> ColumnsAsync(CategoryTestDatabase db) => db.RowsAsync(db.Provider == "PostgreSql"
        ? """SELECT table_name, column_name, data_type, is_nullable FROM information_schema.columns WHERE table_schema = 'public' ORDER BY table_name, column_name"""
        : """SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' ORDER BY TABLE_NAME, COLUMN_NAME""");

    private static Task<string[][]> IdentityAsync(CategoryTestDatabase db) => db.RowsAsync(db.Provider == "PostgreSql"
        ? """
            SELECT seq.seqstart, seq.seqincrement, view.last_value
            FROM pg_sequence seq JOIN pg_class c ON c.oid = seq.seqrelid
            JOIN pg_sequences view ON view.schemaname = 'public' AND view.sequencename = c.relname
            WHERE seq.seqrelid = pg_get_serial_sequence('public."Category"', 'Id')::regclass
            """
        : """
            SELECT CONVERT(bigint, seed_value), CONVERT(bigint, increment_value), CONVERT(bigint, last_value)
            FROM sys.identity_columns WHERE object_id = OBJECT_ID('dbo.Category') AND name = 'Id'
            """);

    private static async Task AssertIdentityAsync(CategoryTestDatabase db)
    {
        var identity = Assert.Single(await IdentityAsync(db));
        Assert.Equal("100", identity[0]);
        Assert.Equal("1", identity[1]);
        var columns = await ColumnsAsync(db);
        Assert.Contains(columns, row => row[0] == "Category" && row[1] == "Id" &&
            row[2] == (db.Provider == "PostgreSql" ? "integer" : "int") && row[3] == "NO");
        Assert.Contains(columns, row => row[0] == "PostCategory" && row[1] == "CategoryId" &&
            row[2] == (db.Provider == "PostgreSql" ? "integer" : "int") && row[3] == "NO");
        Assert.DoesNotContain(columns, row => row[1] is "IdInt" or "CategoryIdInt");
    }

    private static Task<string[][]> KeysAsync(CategoryTestDatabase db) => db.RowsAsync(db.Provider == "PostgreSql"
        ? """
            SELECT t.relname, c.conname, pg_get_constraintdef(c.oid)
            FROM pg_constraint c JOIN pg_class t ON t.oid = c.conrelid
            WHERE t.relname IN ('Category', 'PostCategory', 'Post')
            UNION ALL
            SELECT t.relname, i.relname, pg_get_indexdef(i.oid)
            FROM pg_index x JOIN pg_class i ON i.oid = x.indexrelid JOIN pg_class t ON t.oid = x.indrelid
            WHERE t.relname IN ('Category', 'PostCategory', 'Post') AND NOT x.indisprimary
            ORDER BY 1, 2
            """
        : """
            SELECT t.name, k.name, 'PRIMARY KEY ' + i.type_desc COLLATE DATABASE_DEFAULT + ': ' +
                STRING_AGG(CONVERT(nvarchar(max), c.name), ',') WITHIN GROUP (ORDER BY ic.key_ordinal)
            FROM sys.key_constraints k JOIN sys.tables t ON t.object_id = k.parent_object_id
            JOIN sys.indexes i ON i.object_id = t.object_id AND i.index_id = k.unique_index_id
            JOIN sys.index_columns ic ON ic.object_id = t.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c ON c.object_id = t.object_id AND c.column_id = ic.column_id
            WHERE t.name IN ('Category', 'PostCategory', 'Post')
            GROUP BY t.name, k.name, i.type_desc
            UNION ALL
            SELECT t.name, f.name, c.name + ' -> ' + rt.name + '.' + rc.name + ': ' +
                f.delete_referential_action_desc COLLATE DATABASE_DEFAULT + '/' + f.update_referential_action_desc COLLATE DATABASE_DEFAULT
            FROM sys.foreign_keys f JOIN sys.tables t ON t.object_id = f.parent_object_id
            JOIN sys.tables rt ON rt.object_id = f.referenced_object_id
            JOIN sys.foreign_key_columns fc ON fc.constraint_object_id = f.object_id
            JOIN sys.columns c ON c.object_id = t.object_id AND c.column_id = fc.parent_column_id
            JOIN sys.columns rc ON rc.object_id = rt.object_id AND rc.column_id = fc.referenced_column_id
            WHERE t.name IN ('Category', 'PostCategory', 'Post')
            UNION ALL
            SELECT t.name, i.name, 'INDEX: ' + c.name + CASE WHEN ic.is_descending_key = 1 THEN ' DESC' ELSE ' ASC' END
            FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id
            JOIN sys.index_columns ic ON ic.object_id = t.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c ON c.object_id = t.object_id AND c.column_id = ic.column_id
            WHERE t.name IN ('Category', 'PostCategory', 'Post') AND i.is_primary_key = 0
            ORDER BY 1, 2, 3
            """);

    private static async Task LegacySchemaAsync(CategoryTestDatabase db, bool populated, bool customNames = false)
    {
        var guid = db.Provider == "PostgreSql" ? "uuid" : "uniqueidentifier";
        var timestamp = db.Provider == "PostgreSql" ? "timestamp with time zone" : "datetime2(7)";
        var text = db.Provider == "PostgreSql" ? "text" : "nvarchar(max)";
        var defaultGuid = db.Provider == "SqlServer" ? """CONSTRAINT "DF_Category_Id" DEFAULT NEWID()""" : "";
        var categoryPk = customNames ? "Retained_Category_PK" : "PK_Category";
        var linkPk = customNames ? "Retained_PostCategory_PK" : "PK_PostCategory";
        var categoryFk = customNames ? "Retained_Category_FK" : "FK_PostCategory_Category";
        var categoryIndex = customNames ? "Retained_Category_Index" : "IX_PostCategory_CategoryId";
        var categoryActions = customNames ? "ON DELETE NO ACTION ON UPDATE CASCADE" : "ON DELETE CASCADE";
        var indexDirection = customNames ? "DESC" : "ASC";
        await db.ExecuteAsync($$"""
            CREATE TABLE "Category" (
                "Id" {{guid}} NOT NULL {{defaultGuid}},
                "Slug" varchar(64), "DisplayName" varchar(64), "Note" varchar(128),
                CONSTRAINT "{{categoryPk}}" PRIMARY KEY ("Id"));
            CREATE TABLE "Post" (
                "Id" {{guid}} NOT NULL, "Title" varchar(128), "Slug" varchar(128),
                "PostContent" {{text}}, "CreateTimeUtc" {{timestamp}} NOT NULL,
                "LastModifiedUtc" {{timestamp}}, "PubDateUtc" {{timestamp}}, "RouteLink" varchar(256),
                CONSTRAINT "PK_Post" PRIMARY KEY ("Id"));
            CREATE TABLE "PostCategory" (
                "PostId" {{guid}} NOT NULL, "CategoryId" {{guid}} NOT NULL,
                CONSTRAINT "{{linkPk}}" PRIMARY KEY ("PostId", "CategoryId"),
                CONSTRAINT "{{categoryFk}}" FOREIGN KEY ("CategoryId") REFERENCES "Category" ("Id") {{categoryActions}},
                CONSTRAINT "FK_PostCategory_Post" FOREIGN KEY ("PostId") REFERENCES "Post" ("Id") ON DELETE CASCADE);
            CREATE INDEX "{{categoryIndex}}" ON "PostCategory" ("CategoryId" {{indexDirection}});
            INSERT INTO "Post" ("Id", "Title", "Slug", "PostContent", "CreateTimeUtc", "LastModifiedUtc", "PubDateUtc", "RouteLink") VALUES
                ('{{FirstPost}}', 'First article', 'first-article', '<p>Original content</p>', '2026-01-02T03:04:05.123456', '2026-02-03T04:05:06.654321', '2026-01-02T03:04:05.123456', '2026/1/2/first-article'),
                ('{{SecondPost}}', 'Second article', 'second-article', '<p>Second content</p>', '2026-01-03T03:04:05.123456', NULL, NULL, NULL);
            """);
        if (populated)
            await db.ExecuteAsync($$"""
                INSERT INTO "Category" ("Id", "Slug", "DisplayName", "Note") VALUES
                    ('{{FirstCategory}}', 'cloud', 'Cloud, Development', 'Original cloud note'),
                    ('{{SecondCategory}}', 'life', 'Life', 'Original life note');
                INSERT INTO "PostCategory" ("PostId", "CategoryId") VALUES
                    ('{{FirstPost}}', '{{FirstCategory}}'), ('{{FirstPost}}', '{{SecondCategory}}'), ('{{SecondPost}}', '{{SecondCategory}}');
                """);
    }

    private static PostEditModel PostPayload(int[] categories) => new()
    {
        Title = "Article", Slug = "article", Author = "Author", EditorContent = "Article content",
        Abstract = "Article summary", ContentType = "html", LanguageCode = "en-us", Tags = "",
        Keywords = "", PostStatus = PostStatus.Published, SelectedCatIds = categories, FeedIncluded = true
    };
}
