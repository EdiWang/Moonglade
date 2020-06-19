using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Core;
using Moonglade.Web.Authentication;

namespace Moonglade.Web.Controllers
{
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationOptions.DefaultScheme)]
    [Route("api/graph")]
    [ApiController]
    public class BlogGraphController : ControllerBase
    {
        private readonly ILogger<BlogGraphController> _logger;

        public BlogGraphController(ILogger<BlogGraphController> logger)
        {
            _logger = logger;
        }

        public string Version()
        {
            return Utils.AppVersion;
        }
    }
}
