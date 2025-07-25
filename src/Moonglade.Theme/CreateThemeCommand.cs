﻿using LiteBus.Commands.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;
using System.Text.Json;

namespace Moonglade.Theme;

public record CreateThemeCommand(string Name, IDictionary<string, string> Rules) : ICommand<int>;

public class CreateThemeCommandHandler(MoongladeRepository<BlogThemeEntity> repo) : ICommandHandler<CreateThemeCommand, int>
{
    public async Task<int> HandleAsync(CreateThemeCommand request, CancellationToken ct)
    {
        var (name, dictionary) = request;
        if (await repo.AnyAsync(new ThemeByNameSpec(name.Trim()), ct)) return -1;

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