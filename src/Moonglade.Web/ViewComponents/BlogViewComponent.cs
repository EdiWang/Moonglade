using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Moonglade.Web.ViewComponents
{
    public abstract class BlogViewComponent : ViewComponent
    {
        protected readonly ILogger<BlogViewComponent> Logger;

        protected BlogViewComponent(ILogger<BlogViewComponent> logger = null)
        {
            if (null != logger)
            {
                Logger = logger;
            }
        }
    }
}
