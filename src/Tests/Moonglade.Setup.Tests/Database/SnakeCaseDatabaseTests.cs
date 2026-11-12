using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.PostgreSql;
using Moq;
using Npgsql.NameTranslation;
using System.Data.Common;

namespace Moonglade.Setup.Tests.Database;

public class SnakeCaseDatabaseTests(DisposableCategoryDatabases databases) : IClassFixture<DisposableCategoryDatabases>
{
    [DisposableDatabaseTheory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public async Task Migration_PreservesAllRowsDependenciesAndIdentity_AndCanRunTwice(string provider)
    {
        await using var database = await databases.CreateAsync(provider);
        await using var legacy = provider == "PostgreSql"
            ? new LegacyPostgreSqlBlogDbContext(new DbContextOptionsBuilder<LegacyPostgreSqlBlogDbContext>()
                .UseNpgsql(database.Connection).Options)
            : database.Context();
        await legacy.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        await Seed.SeedAsync(legacy, Mock.Of<ILogger>());
        var post = await legacy.Post.SingleAsync(TestContext.Current.CancellationToken);
        post.Tags = await legacy.Tag.ToListAsync(TestContext.Current.CancellationToken);
        legacy.Comment.Add(new CommentEntity
        {
            Id = Guid.NewGuid(),
            PostId = post.Id,
            Username = "Reader",
            Email = "reader@example.test",
            IPAddress = "192.0.2.1",
            CommentContent = "Preserved comment",
            CreateTimeUtc = DateTime.UtcNow
        });
        legacy.PostCategory.Add(new PostCategoryEntity { PostId = post.Id, CategoryId = 100 });
        await legacy.SaveChangesAsync(TestContext.Current.CancellationToken);
        var before = await ContentAsync(database, legacy, snakeCase: false);
        var dependencies = provider == "PostgreSql" ? await DependenciesAsync(database) : [];

        await database.MigrateAsync("snake-case-names.sql");

        Assert.Equal(before, await ContentAsync(database, legacy, snakeCase: provider == "PostgreSql"));
        if (provider == "PostgreSql") Assert.Equal(dependencies, await DependenciesAsync(database));
        var schema = await SchemaAsync(database);
        await database.MigrateAsync("snake-case-names.sql");
        Assert.Equal(schema, await SchemaAsync(database));
        Assert.Equal(before, await ContentAsync(database, legacy, snakeCase: provider == "PostgreSql"));
        if (provider == "PostgreSql") Assert.Equal(dependencies, await DependenciesAsync(database));

        await using var current = database.Context();
        Assert.Equal("192.0.2.1", (await current.Comment.SingleAsync(TestContext.Current.CancellationToken)).IPAddress);
        var article = await current.Post.Include(p => p.Tags).Include(p => p.PostCategory)
            .SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(post.PostContent, article.PostContent);
        Assert.Equal(2, article.Tags.Count);
        Assert.Equal(100, Assert.Single(article.PostCategory).CategoryId);
        var category = new CategoryEntity { DisplayName = "Next", Slug = "next", Note = "Next category" };
        current.Category.Add(category);
        await current.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.Equal(101, category.Id);
        await current.Category.Where(c => c.Id == 100).ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        Assert.Empty(await current.PostCategory.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, await current.Post.CountAsync(TestContext.Current.CancellationToken));
    }

    [DisposableDatabaseTheory]
    [InlineData("table-conflict")]
    [InlineData("column-conflict")]
    [InlineData("missing-table")]
    [InlineData("uuid-category")]
    public async Task Migration_InvalidSchema_RollsBackEveryRename(string fault)
    {
        await using var database = await databases.CreateAsync("PostgreSql");
        await using var legacy = new LegacyPostgreSqlBlogDbContext(
            new DbContextOptionsBuilder<LegacyPostgreSqlBlogDbContext>().UseNpgsql(database.Connection).Options);
        await legacy.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        await database.ExecuteAsync(fault switch
        {
            "table-conflict" => "CREATE TABLE public.post (id integer);",
            "column-conflict" => """ALTER TABLE public."Post" ADD COLUMN title text;""",
            "missing-table" => """DROP TABLE public."Widget";""",
            "uuid-category" => """
                ALTER TABLE public."PostCategory" DROP CONSTRAINT "FK_PostCategory_Category";
                ALTER TABLE public."Category" ALTER COLUMN "Id" DROP IDENTITY;
                ALTER TABLE public."Category" ALTER COLUMN "Id" TYPE uuid USING NULL::uuid;
                """,
            _ => throw new ArgumentOutOfRangeException(nameof(fault))
        });
        var schema = await SchemaAsync(database);

        var exception = await Assert.ThrowsAnyAsync<DbException>(() => database.MigrateAsync("snake-case-names.sql"));

        Assert.Contains(fault switch
        {
            "missing-table" => "missing",
            "uuid-category" => "integer category IDs",
            _ => "conflict"
        }, exception.Message);
        Assert.Equal(schema, await SchemaAsync(database));
    }

    private static async Task<string[]> ContentAsync(CategoryTestDatabase database, BlogDbContext legacy, bool snakeCase)
    {
        var translator = new NpgsqlSnakeCaseNameTranslator();
        var content = new List<string>();
        foreach (var entity in legacy.Model.GetEntityTypes().OrderBy(e => e.GetTableName()))
        {
            var originalTable = entity.GetTableName()!;
            var table = snakeCase ? translator.TranslateTypeName(originalTable) : originalTable;
            var columns = string.Join(", ", entity.GetProperties().OrderBy(p => p.Name).Select(property =>
            {
                var name = property.GetColumnName();
                var column = snakeCase ? translator.TranslateMemberName(name) : name;
                return $"\"{column}\" AS \"{name}\"";
            }));
            var rows = await database.RowsAsync($"SELECT {columns} FROM \"{table}\"");
            content.AddRange(rows.Select(row => originalTable + ":" + System.Text.Json.JsonSerializer.Serialize(row)).Order());
        }
        return content.ToArray();
    }

    private static Task<string[][]> SchemaAsync(CategoryTestDatabase database) => database.RowsAsync(
        """
        SELECT table_name, column_name, data_type, is_nullable FROM information_schema.columns
        WHERE table_schema IN ('public', 'dbo') ORDER BY table_name, ordinal_position
        """);

    private static Task<string[][]> DependenciesAsync(CategoryTestDatabase database) => database.RowsAsync(
        """
        SELECT 'constraint', oid::text, conname, conrelid::text, confrelid::text,
            conkey::text, confkey::text, confdeltype::text, confupdtype::text
        FROM pg_constraint WHERE connamespace = 'public'::regnamespace
        UNION ALL
        SELECT 'index', indexrelid::text, '', indrelid::text, '', indkey::text, '',
            indisunique::text, indisvalid::text
        FROM pg_index WHERE indrelid IN (SELECT oid FROM pg_class WHERE relnamespace = 'public'::regnamespace)
        UNION ALL
        SELECT 'sequence', s.seqrelid::text, c.relname, s.seqstart::text, s.seqincrement::text,
            v.last_value::text, '', '', ''
        FROM pg_sequence s JOIN pg_class c ON c.oid = s.seqrelid
        JOIN pg_sequences v ON v.schemaname = 'public' AND v.sequencename = c.relname
        ORDER BY 1, 2
        """);

    private sealed class LegacyPostgreSqlBlogDbContext(DbContextOptions options) : PostgreSqlBlogDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<OwnerAccountEntity>();
            modelBuilder.Ignore<OwnerAvatarEntity>();
            modelBuilder.Ignore<OwnerOidcEntity>();
            modelBuilder.Ignore<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey>();
            foreach (var set in typeof(BlogDbContext).GetProperties()
                         .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)))
            {
                var entity = modelBuilder.Model.FindEntityType(set.PropertyType.GenericTypeArguments[0]);
                if (entity is null) continue;
                entity.SetTableName(set.Name);
                foreach (var property in entity.GetProperties()) property.SetColumnName(property.Name);
            }
        }
    }
}
