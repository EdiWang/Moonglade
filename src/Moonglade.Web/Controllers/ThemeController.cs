using MediatR;
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
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;

namespace Moonglade.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ThemeController : ControllerBase
    {
        private readonly IMediator _mediator;

        private readonly IThemeService _themeService;
        private readonly IBlogCache _cache;
        private readonly IBlogConfig _blogConfig;

        public ThemeController(IMediator mediator, IThemeService themeService, IBlogCache cache, IBlogConfig blogConfig)
        {
            _mediator = mediator;

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

                    // Fall back to default theme for migration
                    if (_blogConfig.GeneralSettings.ThemeId == 0)
                    {
                        _blogConfig.GeneralSettings.ThemeId = 1;
                        await _blogConfig.SaveAsync(_blogConfig.GeneralSettings);
                    }

                    var data = await _mediator.Send(new GetStyleSheetQuery(_blogConfig.GeneralSettings.ThemeId));
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
        [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { CacheDivision.General, "theme" })]
        public async Task<IActionResult> Create(CreateThemeRequest request)
        {
            var dic = new Dictionary<string, string>
            {
                { "--accent-color1", request.AccentColor1 },
                { "--accent-color2", request.AccentColor2 },
                { "--accent-color3", request.AccentColor3 }
            };

            var id = await _themeService.Create(request.Name, dic);
            if (id == 0) return Conflict("Theme with same name already exists");

            return Ok(id);
        }

        [Authorize]
        [HttpDelete("{id:int}")]
        [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Delete))]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { CacheDivision.General, "theme" })]
        public async Task<IActionResult> Delete([Range(1, int.MaxValue)] int id)
        {
            var oc = await _mediator.Send(new DeleteThemeCommand(id));
            return oc switch
            {
                OperationCode.ObjectNotFound => NotFound(),
                OperationCode.Canceled => BadRequest("Can not delete system theme"),
                _ => NoContent()
            };
        }
    }
}
