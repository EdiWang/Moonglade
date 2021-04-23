using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;

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
