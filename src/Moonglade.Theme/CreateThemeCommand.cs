using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System.Text.Json;

namespace Moonglade.Theme;

public record CreateThemeCommand(string Name, IDictionary<string, string> Rules) : IRequest<int>;

public class CreateThemeCommandHandler : IRequestHandler<CreateThemeCommand, int>
{
    private readonly IRepository<BlogThemeEntity> _themeRepo;

    public CreateThemeCommandHandler(IRepository<BlogThemeEntity> themeRepo)
    {
        _themeRepo = themeRepo;
    }

    public async Task<int> Handle(CreateThemeCommand request, CancellationToken cancellationToken)
    {
        var (name, dictionary) = request;
        if (_themeRepo.Any(p => p.ThemeName == name.Trim())) return 0;

        var rules = JsonSerializer.Serialize(dictionary);
        var blogTheme = new BlogThemeEntity
        {
            ThemeName = name.Trim(),
            CssRules = rules,
            ThemeType = ThemeType.User
        };

        await _themeRepo.AddAsync(blogTheme);
        return blogTheme.Id;
    }
}