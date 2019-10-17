using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Moonglade.Web.ViewComponents
{
    public abstract class MoongladeViewComponent : ViewComponent
    {
        protected readonly ILogger<MoongladeViewComponent> Logger;

        protected MoongladeViewComponent(ILogger<MoongladeViewComponent> logger = null)
        {
            if (null != logger)
            {
                Logger = logger;
            }
        }
    }
}
