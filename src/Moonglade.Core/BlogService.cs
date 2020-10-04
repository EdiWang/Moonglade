using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class BlogService
    {
        protected readonly ILogger<BlogService> Logger;

        protected readonly AppSettings AppSettings;

        public BlogService(ILogger<BlogService> logger = null,
            IOptions<AppSettings> settings = null)
        {
            if (null != settings) AppSettings = settings.Value;
            if (null != logger) Logger = logger;
        }
    }
}
