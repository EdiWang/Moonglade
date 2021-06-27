using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Theme
{
    public interface IThemeService
    {
        Task<int> Create(string name, IDictionary<string, string> cssRules);
        Task<IReadOnlyList<ThemeSegment>> GetAllSegment();
        Task<string> GetStyleSheet(int id);
        Task<OperationCode> Delete(int id);
    }

    public struct ThemeSegment
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ThemeService : IThemeService
    {
        private readonly IRepository<BlogThemeEntity> _themeRepo;

        public ThemeService(IRepository<BlogThemeEntity> themeRepo)
        {
            _themeRepo = themeRepo;
        }

        public Task<IReadOnlyList<ThemeSegment>> GetAllSegment()
        {
            return _themeRepo.SelectAsync(p => new ThemeSegment()
            {
                Id = p.Id,
                Name = p.ThemeName
            });
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

        public async Task<string> GetStyleSheet(int id)
        {
            var theme = await _themeRepo.GetAsync(id);
            if (null == theme) return null;

            if (string.IsNullOrWhiteSpace(theme.CssRules))
            {
                throw new InvalidDataException($"Theme id '{id}' is having empty CSS Rules");
            }

            var rules = JsonSerializer.Deserialize<IDictionary<string, string>>(theme.CssRules);
            if (null == rules)
            {
                throw new InvalidDataException($"Theme id '{id}' CssRules is not a valid json");
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

        public async Task<OperationCode> Delete(int id)
        {
            var theme = await _themeRepo.GetAsync(id);
            if (null == theme) return OperationCode.ObjectNotFound;
            if (theme.ThemeType == ThemeType.System) return OperationCode.Canceled;

            await _themeRepo.DeleteAsync(id);
            return OperationCode.Done;
        }
    }
}
