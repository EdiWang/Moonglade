using LiteBus.Commands.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;
using System.Text.Json;

namespace Moonglade.Theme;

public record CreateThemeCommand(string Name, IDictionary<string, string> Rules) : ICommand<int>;

public class CreateThemeCommandHandler(BlogDbContext db) : ICommandHandler<CreateThemeCommand, int>
{
    public async Task<int> HandleAsync(CreateThemeCommand request, CancellationToken ct)
    {
        var (name, dictionary) = request;
        var trimmedName = name.Trim();
        if (await db.BlogTheme.AnyAsync(t => t.ThemeName == trimmedName, ct)) return -1;

        var rules = JsonSerializer.Serialize(dictionary);
        var entity = new BlogThemeEntity
        {
            ThemeName = trimmedName,
            CssRules = rules,
            ThemeType = ThemeType.User
        };

        await db.BlogTheme.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }
}