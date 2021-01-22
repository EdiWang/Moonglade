using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.FriendLink;

namespace Moonglade.Web.ViewComponents
{
    public class FriendLinkViewComponent : ViewComponent
    {
        private readonly ILogger<FriendLinkViewComponent> _logger;
        private readonly IFriendLinkService _friendLinkService;

        public FriendLinkViewComponent(ILogger<FriendLinkViewComponent> logger,
            IFriendLinkService friendLinkService)
        {
            _logger = logger;
            _friendLinkService = friendLinkService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var links = await _friendLinkService.GetAllAsync();
                return View(links ?? new List<FriendLink.Link>());
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
