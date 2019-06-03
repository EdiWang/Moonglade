using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Web.ViewComponents
{
    public class FriendLinkViewComponent : MoongladeViewComponent
    {
        private readonly FriendLinkService _friendLinkService;

        public FriendLinkViewComponent(ILogger<FriendLinkViewComponent> logger, IOptions<AppSettings> settings,
            FriendLinkService friendLinkService)
            : base(logger, settings)
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

                // should not block website
                return View(new List<FriendLink>());
            }
        }
    }
}
