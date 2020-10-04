using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Core;
using Moonglade.Model;

namespace Moonglade.Web.ViewComponents
{
    public class FriendLinkViewComponent : ViewComponent
    {
        private readonly ILogger<FriendLinkViewComponent> _logger;
        private readonly FriendLinkService _friendLinkService;

        public FriendLinkViewComponent(ILogger<FriendLinkViewComponent> logger,
            FriendLinkService friendLinkService)
        {
            _logger = logger;
            _friendLinkService = friendLinkService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var links = await _friendLinkService.GetAllAsync();
                return View(links ?? new List<FriendLink>());
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error Reading FriendLink.");

                ViewBag.ComponentErrorMessage = e.Message;
                return View("~/Views/Shared/ComponentError.cshtml");
            }
        }
    }
}
