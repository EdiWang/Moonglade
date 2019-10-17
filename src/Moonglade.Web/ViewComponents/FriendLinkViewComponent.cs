using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Core;
using Moonglade.Model;

namespace Moonglade.Web.ViewComponents
{
    public class FriendLinkViewComponent : MoongladeViewComponent
    {
        private readonly FriendLinkService _friendLinkService;

        public FriendLinkViewComponent(ILogger<FriendLinkViewComponent> logger,
            FriendLinkService friendLinkService)
            : base(logger)
        {
            _friendLinkService = friendLinkService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var response = await _friendLinkService.GetAllFriendLinksAsync();
                return View(response.IsSuccess ? response.Item : new List<FriendLink>());
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error Reading FriendLink.");

                ViewBag.ComponentErrorMessage = e.Message;
                return View("~/Views/Shared/ComponentError.cshtml");
            }
        }
    }
}
