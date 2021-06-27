using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Caching;
using Moonglade.Caching.Filters;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Theme;
using Moonglade.Web.Models.Settings;
using NUglify;

namespace Moonglade.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ThemeController : ControllerBase
    {
        private readonly IThemeService _themeService;
        private readonly IBlogCache _cache;
        private readonly IBlogConfig _blogConfig;

        public ThemeController(IThemeService themeService, IBlogCache cache, IBlogConfig blogConfig)
        {
            _themeService = themeService;
            _cache = cache;
            _blogConfig = blogConfig;
        }

        [HttpGet("/theme.css")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(List<UglifyError>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Css()
        {
            try
            {
                var css = await _cache.GetOrCreateAsync(CacheDivision.General, "theme", async entry =>
                {
                    entry.SlidingExpiration = TimeSpan.FromMinutes(20);

                    // Fall back to default theme
                    if (string.IsNullOrWhiteSpace(_blogConfig.GeneralSettings.ThemeName))
                    {
                        _blogConfig.GeneralSettings.ThemeName = "Word Blue";
                        await _blogConfig.SaveAsync(_blogConfig.GeneralSettings);
                    }

                    var data = await _themeService.GetStyleSheet(_blogConfig.GeneralSettings.ThemeName);
                    return data;
                });

                if (css == null) return NotFound();

                var uCss = Uglify.Css(css);
                if (uCss.HasErrors) return Conflict(uCss.Errors);

                return Content(uCss.Code, "text/css");
            }
            catch (InvalidDataException e)
            {
                return Conflict(e.Message);
            }
        }

        [Authorize]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { CacheDivision.General, "theme" })]
        public async Task<IActionResult> CreateTheme(CreateThemeRequest request)
        {
            var dic = new Dictionary<string, string>
            {
                { "--accent-color1", request.AccentColor1 },
                { "--accent-color2", request.AccentColor2 },
                { "--accent-color3", request.AccentColor3 }
            };

            await _themeService.Create(request.Name, dic);
            return NoContent();
        }

        [Authorize]
        [HttpDelete]
        [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Delete))]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { CacheDivision.General, "theme" })]
        public async Task<IActionResult> DeleteTheme([Range(1, int.MaxValue)] int id)
        {
            var oc = await _themeService.Delete(id);
            return oc switch
            {
                OperationCode.ObjectNotFound => NotFound(),
                OperationCode.Canceled => BadRequest("Can not delete system theme"),
                _ => NoContent()
            };
        }
    }
}
