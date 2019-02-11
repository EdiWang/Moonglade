using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
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
                logger.Info($"Moonglade is starting, hail Microsoft! \n" +
                            $"--------------------------------------------------------\n" +
                            $" Version: {Utils.AppVersion} \n" +
                            $" Directory: {Environment.CurrentDirectory} \n" +
                            $" x64Process: {Environment.Is64BitProcess} \n" +
                            $" OSVersion: {System.Runtime.InteropServices.RuntimeInformation.OSDescription} \n" +
                            $" AppDomain: {AppDomain.CurrentDomain.FriendlyName} \n" +
                            $" UserName: {Environment.UserName} \n" +
                            $"--------------------------------------------------------");

                CreateWebHostBuilder(args).Build().Run();
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

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .CaptureStartupErrors(true)
                .UseApplicationInsights()
                .ConfigureKestrel(c => c.AddServerHeader = false)
                .UseIISIntegration()
                .UseStartup<Startup>()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Trace);
                }).UseNLog();
    }
}
