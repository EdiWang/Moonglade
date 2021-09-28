using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Theme
{
    public class CreateThemeCommand : IRequest<int>
    {
        public CreateThemeCommand(string name, IDictionary<string, string> rules)
        {
            Name = name;
            Rules = rules;
        }

        public string Name { get; set; }

        public IDictionary<string, string> Rules { get; set; }
    }

    public class CreateThemeCommandHandler : IRequestHandler<CreateThemeCommand, int>
    {
        private readonly IRepository<BlogThemeEntity> _themeRepo;

        public CreateThemeCommandHandler(IRepository<BlogThemeEntity> themeRepo)
        {
            _themeRepo = themeRepo;
        }

        public async Task<int> Handle(CreateThemeCommand request, CancellationToken cancellationToken)
        {
            if (_themeRepo.Any(p => p.ThemeName == request.Name.Trim())) return 0;

            var rules = JsonSerializer.Serialize(request.Rules);
            var blogTheme = new BlogThemeEntity
            {
                ThemeName = request.Name.Trim(),
                CssRules = rules,
                ThemeType = ThemeType.User
            };

            await _themeRepo.AddAsync(blogTheme);
            return blogTheme.Id;
        }
    }
}
