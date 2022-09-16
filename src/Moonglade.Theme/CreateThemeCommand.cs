using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System.Text.Json;

namespace Moonglade.Theme;

public record CreateThemeCommand(string Name, IDictionary<string, string> Rules) : IRequest<int>;

public class CreateThemeCommandHandler : IRequestHandler<CreateThemeCommand, int>
{
    private readonly IRepository<BlogThemeEntity> _repo;

    public CreateThemeCommandHandler(IRepository<BlogThemeEntity> repo) => _repo = repo;

    public async Task<int> Handle(CreateThemeCommand request, CancellationToken ct)
    {
        var (name, dictionary) = request;
        if (await _repo.AnyAsync(p => p.ThemeName == name.Trim(), ct)) return 0;

        var rules = JsonSerializer.Serialize(dictionary);
        var blogTheme = new BlogThemeEntity
        {
            ThemeName = name.Trim(),
            CssRules = rules,
            ThemeType = ThemeType.User
        };

        await _repo.AddAsync(blogTheme, ct);
        return blogTheme.Id;
    }
}