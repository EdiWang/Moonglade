using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System.Text.Json;

namespace Moonglade.Theme;

public record CreateThemeCommand(string Name, IDictionary<string, string> Rules) : IRequest<int>;

public class CreateThemeCommandHandler(IRepository<BlogThemeEntity> repo) : IRequestHandler<CreateThemeCommand, int>
{
    public async Task<int> Handle(CreateThemeCommand request, CancellationToken ct)
    {
        var (name, dictionary) = request;
        if (await repo.AnyAsync(p => p.ThemeName == name.Trim(), ct)) return 0;

        var rules = JsonSerializer.Serialize(dictionary);
        var entity = new BlogThemeEntity
        {
            ThemeName = name.Trim(),
            CssRules = rules,
            ThemeType = ThemeType.User
        };

        await repo.AddAsync(entity, ct);
        return entity.Id;
    }
}