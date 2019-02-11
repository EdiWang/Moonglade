using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.AzureApplicationInsights;
using Moonglade.Data;
using Moonglade.Model.Settings;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [Route("admin")]
    public class AdminController : MoongladeController
    {
        private readonly MetricsReader _metricsReader;

        public AdminController(MoongladeDbContext context,
            ILogger<AdminController> logger,
            IOptions<AppSettings> settings,
            IConfiguration configuration,
            IHttpContextAccessor accessor, MetricsReader metricsReader)
            : base(context, logger, settings, configuration, accessor)
        {
            _metricsReader = metricsReader;
        }

        [HttpGet("signin")]
        [AllowAnonymous]
        public IActionResult SignIn()
        {
            var redirectUrl = Url.Action(nameof(PostController.Index), "Post");
            return Challenge(
                new AuthenticationProperties { RedirectUri = redirectUrl },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet("signout")]
        public IActionResult SignOut()
        {
            var callbackUrl = Url.Action(nameof(SignedOut), "Admin", values: null, protocol: Request.Scheme);
            return SignOut(
                new AuthenticationProperties { RedirectUri = callbackUrl },
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet("signedout")]
        [AllowAnonymous]
        public IActionResult SignedOut()
        {
            return RedirectToAction(nameof(PostController.Index), "Post");
        }

        [HttpGet("accessdenied")]
        public IActionResult AccessDenied()
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }

        // Keep session from expire when writing a very long post
        [Route("keep-alive")]
        public IActionResult KeepAlive(string nonce)
        {
            return Json(new
            {
                ServerTime = DateTime.UtcNow,
                Nonce = nonce
            });
        }

        [Route("getpv")]
        public async Task<int> GetPageViewsForLast24Hours()
        {
            var response = await _metricsReader.GetP1DMetrics(MetricId.PageViewsCount, MetricAggregation.Sum);
            if (null != response)
            {
                return (int)response.value[MetricId.PageViewsCount].sum;
            }
            return -1;
        }

        [Route("getrequestdurationavg")]
        public async Task<float> GetRequestDurationAvgForLast24Hours()
        {
            var response = await _metricsReader.GetP1DMetrics(MetricId.RequestsDuration, MetricAggregation.Average);
            if (null != response)
            {
                return (float)response.value[MetricId.RequestsDuration].avg;
            }
            return -1.00f;
        }

        [Route("getserverexceptions")]
        public async Task<int> GetServerExceptionsForLast24Hours()
        {
            var response = await _metricsReader.GetP1DMetrics(MetricId.ServerExceptions, MetricAggregation.Sum);
            if (null != response)
            {
                return (int)response.value[MetricId.ServerExceptions].sum;
            }
            return -1;
        }

        [Route("getclientexceptions")]
        public async Task<int> GetClientExceptionsForLast24Hours()
        {
            var response = await _metricsReader.GetP1DMetrics(MetricId.ClientExceptions, MetricAggregation.Sum);
            if (null != response)
            {
                return (int)response.value[MetricId.ClientExceptions].sum;
            }
            return -1;
        }

        [Route("getprocessmemory")]
        public async Task<float> GetProcessMemoryForLast24Hours()
        {
            var response = await _metricsReader.GetP1DMetrics(MetricId.ProcessMemory, MetricAggregation.Average);
            if (null != response)
            {
                return (float)response.value[MetricId.ProcessMemory].avg;
            }
            return -1.00f;
        }
    }
}