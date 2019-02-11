using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Model.Settings;

namespace Moonglade.Web.ViewComponents
{
    public class FriendLinkViewComponent : MoongladeViewComponent
    {
        private readonly FriendLinkService _friendLinkService;

        public FriendLinkViewComponent(ILogger<FriendLinkViewComponent> logger, IOptions<AppSettings> settings,
            FriendLinkService friendLinkService)
            : base(logger, settings: settings)
        {
            _friendLinkService = friendLinkService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            await Task.CompletedTask;

            try
            {
                var response = _friendLinkService.GetAllFriendLinks();
                return View(response.IsSuccess ? response.Item : new List<Data.Entities.FriendLink>());
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error Reading FriendLink.");

                // should not block website
                return View(new List<Data.Entities.FriendLink>());
            }
        }
    }
}
