using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class BlogService
    {
        protected readonly ILogger Logger;

        protected readonly AppSettings AppSettings;

        public BlogService(ILogger logger = null,
            IOptions<AppSettings> settings = null)
        {
            if (settings is not null) AppSettings = settings.Value;
            if (logger is not null) Logger = logger;
        }
    }
}
