using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Moonglade.Data.Entities;

public class BlogThemeEntity
{
    public int Id { get; set; }
    public string ThemeName { get; set; }
    public string CssRules { get; set; }
    public string AdditionalProps { get; set; }
    public ThemeType ThemeType { get; set; }

    [NotMapped]
    public Dictionary<string, string> CssRulesDictionary
    {
        get
        {
            if (string.IsNullOrWhiteSpace(CssRules)) return new();
            return JsonSerializer.Deserialize<Dictionary<string, string>>(CssRules);
        }
    }
}

public enum ThemeType
{
    System = 0,
    User = 1
}