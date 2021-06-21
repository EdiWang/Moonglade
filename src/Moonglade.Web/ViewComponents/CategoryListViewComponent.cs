using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Core;

namespace Moonglade.Web.ViewComponents
{
    public class CategoryListViewComponent : ViewComponent
    {
        private readonly ICategoryService _catService;

        public CategoryListViewComponent(ICategoryService catService)
        {
            _catService = catService;
        }

        public async Task<IViewComponentResult> InvokeAsync(bool isMenu)
        {
            try
            {
                var cats = await _catService.GetAllAsync();
                return isMenu ? View("CatMenu", cats) : View(cats);
            }
            catch (Exception e)
            {
                return Content(e.Message);
            }
        }
    }
}
