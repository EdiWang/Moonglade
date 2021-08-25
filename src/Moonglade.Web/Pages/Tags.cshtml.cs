using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Pages
{
    public class TagsModel : PageModel
    {
        private readonly ITagService _tagService;
        public IReadOnlyList<KeyValuePair<Tag, int>> Tags { get; set; }

        public TagsModel(ITagService tagService)
        {
            _tagService = tagService;
        }

        public async Task OnGet()
        {
            var tags = await _tagService.GetTagCountList();
            Tags = tags;
        }
    }
}
