using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Data;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class MoongladeService
    {
        protected MoongladeDbContext Context;

        protected readonly ILogger<MoongladeService> Logger;

        protected readonly AppSettings AppSettings;

        public MoongladeService(MoongladeDbContext context = null, ILogger<MoongladeService> logger = null,
            IOptions<AppSettings> settings = null)
        {
            if (null != context) Context = context;
            if (null != settings) AppSettings = settings.Value;
            if (null != logger) Logger = logger;
        }
    }
}
