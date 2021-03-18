using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;

namespace Moonglade.Web.Pages.Admin
{
    public class TagsModel : PageModel
    {
        private readonly ITagService _tagService;

        public IReadOnlyList<Tag> Tags { get; set; }

        public TagsModel(ITagService tagService)
        {
            _tagService = tagService;
        }

        public async Task OnGet()
        {
            Tags = await _tagService.GetAll();
        }
    }
}
