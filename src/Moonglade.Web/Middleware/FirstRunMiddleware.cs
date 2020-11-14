using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moonglade.Model;
using Moonglade.Setup;

namespace Moonglade.Web.Middleware
{
    public class FirstRunMiddleware
    {
        private readonly RequestDelegate _next;

        private const string Token = "FIRSTRUN_INIT_SUCCESS";

        public FirstRunMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext,
            IConfiguration configuration,
            IHostApplicationLifetime appLifetime,
            ILogger<FirstRunMiddleware> logger)
        {
            var initFlag = AppDomain.CurrentDomain.GetData(Token);
            if (initFlag is not null)
            {
                // Don't need to check bool true or false, exists means everything
                await _next(httpContext);
                return;
            }

            var conn = configuration.GetConnectionString(Constants.DbConnectionName);
            var setupHelper = new SetupRunner(conn);

            if (!setupHelper.TestDatabaseConnection(exception =>
            {
                logger.LogCritical(exception, $"Error {nameof(SetupRunner.TestDatabaseConnection)}, connection string: {conn}");
            }))
            {
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await httpContext.Response.WriteAsync("Database connection failed. Please see error log, fix it and RESTART this application.");
                appLifetime.StopApplication();
            }
            else
            {
                if (setupHelper.IsFirstRun())
                {
                    try
                    {
                        logger.LogInformation("Initializing first run configuration...");
                        setupHelper.InitFirstRun();
                        logger.LogInformation("Database setup successfully.");
                    }
                    catch (Exception e)
                    {
                        logger.LogCritical(e, e.Message);
                        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await httpContext.Response.WriteAsync("Error initializing first run, please check error log.");
                        appLifetime.StopApplication();
                    }
                }

                AppDomain.CurrentDomain.SetData(Token, true);
                await _next(httpContext);
            }
        }
    }
}
