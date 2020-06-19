using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Core;
using Moonglade.Model;
using Moonglade.Web.Authentication;

namespace Moonglade.Web.Controllers
{
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationOptions.DefaultScheme)]
    [Route("api/graph")]
    [ApiController]
    public class BlogGraphController : ControllerBase
    {
        private readonly ILogger<BlogGraphController> _logger;
        private readonly TagService _tagService;

        public BlogGraphController(
            ILogger<BlogGraphController> logger, 
            TagService tagService)
        {
            _logger = logger;
            _tagService = tagService;
        }

        [HttpGet("version")]
        public string Version()
        {
            return Utils.AppVersion;
        }

        [HttpGet("tags")]
        public async Task<IEnumerable<Tag>> Tags()
        {
            var response = await _tagService.GetAllAsync();
            return response.Item;
        }
    }
}
