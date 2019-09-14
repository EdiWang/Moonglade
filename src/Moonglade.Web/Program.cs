using System;
using Microsoft.AspNetCore;
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
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isProd = environment == Environments.Production;
            var logger = NLogBuilder.ConfigureNLog(isProd ? "nlog.config" : "nlog.debug.config").GetCurrentClassLogger();
            try
            {
                logger.Info($"Moonglade Version {Utils.AppVersion}\n" +
                            "--------------------------------------------------------\n" +
                            $" Directory: {Environment.CurrentDirectory} \n" +
                            $" x64Process: {Environment.Is64BitProcess} \n" +
                            $" OSVersion: {System.Runtime.InteropServices.RuntimeInformation.OSDescription} \n" +
                            $" UserName: {Environment.UserName} \n" +
                            "--------------------------------------------------------");
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
                              .UseIISIntegration()
                              .UseStartup<Startup>()
                              .ConfigureLogging(logging =>
                              {
                                  logging.ClearProviders();
                                  logging.SetMinimumLevel(LogLevel.Trace);
                              }).UseNLog();
                });
    }
}
