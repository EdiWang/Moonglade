using System;
using System.Threading.Tasks;
using Edi.Blog.Pingback.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Model.Settings;

namespace Moonglade.Web.Controllers
{
    [Route("pingback")]
    public class PingbackController : MoongladeController
    {
        private readonly PingbackService _pingbackService;

        public PingbackController(
            ILogger<PingbackController> logger,
            IOptions<AppSettings> settings,
            PingbackService pingbackService)
            : base(logger, settings)
        {
            _pingbackService = pingbackService;
        }

        [HttpPost("")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Index()
        {
            if (!AppSettings.EnablePingBackReceive)
            {
                Logger.LogInformation("Pingback receive is disabled");
                return Forbid();
            }

            var response = await _pingbackService.ProcessReceivedPingback(HttpContext);
            Logger.LogInformation($"Pingback Processor Response: {response.ToString()}");
            return new PingbackResult(response);
        }

        [Authorize]
        [Route("manage")]
        public async Task<IActionResult> Manage()
        {
            var response = await _pingbackService.GetReceivedPingbacksAsync();
            if (response.IsSuccess)
            {
                return View(response.Item);
            }

            return ServerError();
        }

        [Authorize]
        [HttpPost("delete")]
        public IActionResult Delete(Guid pingbackId)
        {
            return _pingbackService.DeleteReceivedPingback(pingbackId).IsSuccess ? Json(pingbackId) : Json(null);
        }
    }
}