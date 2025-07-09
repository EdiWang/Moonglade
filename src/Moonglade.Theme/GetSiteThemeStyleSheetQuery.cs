using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using System.Text.Json;

namespace Moonglade.Theme;

public record GetSiteThemeStyleSheetQuery(int Id) : IRequest<string>;

public class GetStyleSheetQueryHandler(MoongladeRepository<BlogThemeEntity> repo)
    : IRequestHandler<GetSiteThemeStyleSheetQuery, string>
{
    private const int SystemThemeStartId = 100;
    private const int SystemThemeEndId = 110;
    private const int DefaultSystemThemeId = 100;

    public async Task<string> Handle(GetSiteThemeStyleSheetQuery request, CancellationToken ct)
    {
        BlogThemeEntity theme = await GetThemeAsync(request.Id, ct);

        if (theme is null)
            return null;

        if (string.IsNullOrWhiteSpace(theme.CssRules))
        {
            throw new InvalidDataException($"Theme id '{request.Id}' has empty CSS rules.");
        }

        IDictionary<string, string> rules;
        try
        {
            rules = JsonSerializer.Deserialize<IDictionary<string, string>>(theme.CssRules);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Theme id '{request.Id}' has invalid JSON in CssRules.", ex);
        }

        if (rules is null || rules.Count == 0)
        {
            throw new InvalidDataException($"Theme id '{request.Id}' CssRules deserialized to empty or null.");
        }

        // Generate CSS
        var css = $":root {{{string.Join("", rules
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => $"{kv.Key}: {kv.Value};"))}}}";

        return css;
    }

    private async Task<BlogThemeEntity> GetThemeAsync(int id, CancellationToken ct)
    {
        if (id < SystemThemeStartId || id > SystemThemeEndId)
        {
            // Custom theme, fallback to default system theme if not found
            return await repo.GetByIdAsync(id, ct)
                ?? ThemeFactory.GetSystemThemes().FirstOrDefault(t => t.Id == DefaultSystemThemeId);
        }
        // System theme
        return ThemeFactory.GetSystemThemes().FirstOrDefault(t => t.Id == id);
    }
}
