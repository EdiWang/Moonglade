using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Model.Settings;

namespace Moonglade.Web.ViewComponents
{
    public abstract class MoongladeViewComponent : ViewComponent
    {
        protected readonly ILogger<MoongladeViewComponent> Logger;

        protected AppSettings AppSettings { get; set; }

        protected MoongladeViewComponent(ILogger<MoongladeViewComponent> logger = null, IOptions<AppSettings> settings = null)
        {
            if (null != logger)
            {
                Logger = logger;
            }

            if (null != settings)
            {
                AppSettings = settings.Value;
            }
        }
    }
}
