using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class MoongladeService
    {
        protected readonly ILogger<MoongladeService> Logger;

        protected readonly AppSettings AppSettings;

        public MoongladeService(ILogger<MoongladeService> logger = null,
            IOptions<AppSettings> settings = null)
        {
            if (null != settings) AppSettings = settings.Value;
            if (null != logger) Logger = logger;
        }
    }
}
