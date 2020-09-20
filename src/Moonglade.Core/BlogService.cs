using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Model;
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

        public Response TryExecute(Func<Response> func, [CallerMemberName] string callerMemberName = "", object keyParameter = null)
        {
            try
            {
                return func();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error executing {callerMemberName}({keyParameter})");
                return new FailedResponse((int)FaultCode.GeneralException, e.Message);
            }
        }

        public Response<T> TryExecute<T>(Func<Response<T>> func, [CallerMemberName] string callerMemberName = "", object keyParameter = null)
        {
            try
            {
                return func();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error executing {callerMemberName}({keyParameter})");
                return new FailedResponse<T>((int)FaultCode.GeneralException, e.Message);
            }
        }

        public async Task<Response> TryExecuteAsync(Func<Task<Response>> func, [CallerMemberName] string callerMemberName = "", object keyParameter = null)
        {
            try
            {
                return await func();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error executing {callerMemberName}({keyParameter})");
                return new FailedResponse((int)FaultCode.GeneralException, e.Message);
            }
        }

        public async Task<Response<T>> TryExecuteAsync<T>(Func<Task<Response<T>>> func,
            [CallerMemberName] string callerMemberName = "", object keyParameter = null)
        {
            try
            {
                return await func();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error executing {callerMemberName}({keyParameter})");
                return new FailedResponse<T>((int)FaultCode.GeneralException, e.Message);
            }
        }
    }
}
