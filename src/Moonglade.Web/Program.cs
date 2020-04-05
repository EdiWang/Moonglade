using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moonglade.Core;
using NLog.Web;

namespace Moonglade.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            try
            {
                logger.Info($"Moonglade Version {Utils.AppVersion}\n" +
                            $"Directory: {Environment.CurrentDirectory} \n" +
                            $"x64 Process: {Environment.Is64BitProcess} \n" +
                            $"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription} \n" +
                            $"User Name: {Environment.UserName}");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error starting moonglade :(");
                throw;
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.CaptureStartupErrors(true)
                              .ConfigureKestrel(c => c.AddServerHeader = false)
                              .UseStartup<Startup>()
                              .ConfigureLogging(logging =>
                              {
                                  logging.SetMinimumLevel(LogLevel.Trace);
                              });

                    bool runsInDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
                    if (!runsInDocker)
                    {
                        // Because NLog may not be able to write files and find correct directory in a docker conatiner
                        // So only non-container environments are enabled for NLog
                        // Docker can still use Console log
                        webBuilder.UseNLog();
                    }
                });
    }
}
