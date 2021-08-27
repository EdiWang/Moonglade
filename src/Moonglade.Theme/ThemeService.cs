using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Moonglade.Theme
{
    public interface IThemeService
    {
        Task<int> Create(string name, IDictionary<string, string> cssRules);
    }

    public class ThemeService : IThemeService
    {
        private readonly IRepository<BlogThemeEntity> _themeRepo;

        public ThemeService(IRepository<BlogThemeEntity> themeRepo)
        {
            _themeRepo = themeRepo;
        }

        public async Task<int> Create(string name, IDictionary<string, string> cssRules)
        {
            if (_themeRepo.Any(p => p.ThemeName == name.Trim())) return 0;

            var rules = JsonSerializer.Serialize(cssRules);
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
}
