using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System.Text;
using System.Text.Json;

namespace Moonglade.Theme;

public record GetSiteThemeStyleSheetQuery(int Id) : IRequest<string>;
public class GetStyleSheetQueryHandler(IRepository<BlogThemeEntity> repo) : IRequestHandler<GetSiteThemeStyleSheetQuery, string>
{
    public async Task<string> Handle(GetSiteThemeStyleSheetQuery request, CancellationToken ct)
    {
        var theme = await repo.GetAsync(request.Id, ct);
        if (null == theme) return null;

        if (string.IsNullOrWhiteSpace(theme.CssRules))
        {
            throw new InvalidDataException($"Theme id '{request.Id}' is having empty CSS Rules");
        }

        try
        {
            var rules = JsonSerializer.Deserialize<IDictionary<string, string>>(theme.CssRules);

            var sb = new StringBuilder();
            sb.Append(":root {");
            if (rules != null)
            {
                foreach (var (key, value) in rules)
                {
                    if (null != key && null != value)
                    {
                        sb.Append($"{key}: {value};");
                    }
                }
            }

            sb.Append('}');

            return sb.ToString();
        }
        catch (JsonException)
        {
            throw new InvalidDataException($"Theme id '{request.Id}' CssRules is not a valid json");
        }
    }
}