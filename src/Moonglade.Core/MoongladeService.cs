using System;
using System.Runtime.CompilerServices;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Model;
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

        public Response TryExecute(Func<Response> func, [CallerMemberName] string callerMemberName = "", object keyParameter = null)
        {
            try
            {
                return func();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error executing {callerMemberName}({keyParameter})");
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }
    }
}
