using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Theme
{
    public interface IThemeService
    {
        Task<IReadOnlyList<string>> GetAllNames();
        Task<string> GetStyleSheet(string themeName);
        Task Delete(int id);
    }

    public class ThemeService : IThemeService
    {
        private readonly IRepository<BlogThemeEntity> _themeRepo;

        public ThemeService(IRepository<BlogThemeEntity> themeRepo)
        {
            _themeRepo = themeRepo;
        }

        public Task<IReadOnlyList<string>> GetAllNames()
        {
            return _themeRepo.SelectAsync(p => p.ThemeName);
        }

        public async Task<string> GetStyleSheet(string themeName)
        {
            var theme = await _themeRepo.GetAsync(p => p.ThemeName == themeName);
            if (null == theme) return null;

            if (string.IsNullOrWhiteSpace(theme.CssRules))
            {
                throw new InvalidDataException($"'{themeName}' is having empty CSS Rules");
            }

            var rules = JsonSerializer.Deserialize<IDictionary<string, string>>(theme.CssRules);
            if (null == rules)
            {
                throw new InvalidDataException($"'{themeName}' CssRules is not a valid json");
            }

            var sb = new StringBuilder();
            sb.Append(":root {");
            foreach (var (key, value) in rules)
            {
                if (null != key && null != value)
                {
                    sb.Append($"{key}: {value};");
                }
            }
            sb.Append('}');

            return sb.ToString();
        }

        public Task Delete(int id)
        {
            return _themeRepo.DeleteAsync(id);
        }
    }
}
