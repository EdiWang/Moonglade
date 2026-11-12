using Microsoft.EntityFrameworkCore;
using Moonglade.Data.PostgreSql.Configurations;
using Npgsql.NameTranslation;

namespace Moonglade.Data.PostgreSql;

public class PostgreSqlBlogDbContext : BlogDbContext
{
    public PostgreSqlBlogDbContext()
    {
    }

    public PostgreSqlBlogDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new BlogThemeConfiguration());
        modelBuilder.Entity<Entities.CategoryEntity>()
            .Property(e => e.Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: 100, incrementBy: 1);
        Data.Configurations.UtcDateTimeConvention.ConfigureUtcDateTimeColumnType(
            modelBuilder,
            "timestamp with time zone");

        var translator = new NpgsqlSnakeCaseNameTranslator();
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (entity.GetTableName() is not { } tableName) continue;

            entity.SetTableName(translator.TranslateTypeName(tableName));
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(translator.TranslateMemberName(property.GetColumnName()));
            }
        }

        Data.Configurations.OwnerAccountConfiguration.ConfigureConstraints(modelBuilder);
        ConfigureOwnerAvatarConstraint(modelBuilder);
        ConfigureOwnerOidcConstraint(modelBuilder);
    }
}
