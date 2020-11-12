using System;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moonglade.Core;
using Moonglade.Model;

namespace Moonglade.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var info = $"Moonglade Version {Utils.AppVersion}\n" +
                       $"Directory: {Environment.CurrentDirectory} \n" +
                       $"x64 Process: {Environment.Is64BitProcess} \n" +
                       $"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription} \n" +
                       $"User Name: {Environment.UserName}";
            Trace.WriteLine(info);
            Console.WriteLine(info);

            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var loggerFactory = services.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<Program>();

                try
                {
                    var dataDir = CreateDataDirectories();
                    AppDomain.CurrentDomain.SetData(Constants.DataDirectory, dataDir);
                    logger.LogInformation($"Using data directory '{dataDir}'");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Moonglade start up boom boom");
                }
            }

            host.Run();
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
                                  logging.AddAzureWebAppDiagnostics();
                              });
                });

        private static string CreateDataDirectories()
        {
            void DeleteDataFile(string path)
            {
                try
                {
                    if (File.Exists(path)) File.Delete(path);
                }
                catch
                {
                    // Code blow up le what to do?
                    // try catch cover it bu jiu OK le!
                }
            }

            void CleanDataCache(string dataDir)
            {
                var openSearchDataFile = Path.Join($"{dataDir}", $"{Constants.OpenSearchFileName}");
                var opmlDataFile = Path.Join($"{dataDir}", $"{Constants.OpmlFileName}");

                DeleteDataFile(openSearchDataFile);
                DeleteDataFile(opmlDataFile);
            }

            // Use Temp folder as best practice
            // Do NOT create or modify anything under application directory
            // e.g. Azure Deployment using WEBSITE_RUN_FROM_PACKAGE will make website root directory read only.
            var tPath = Path.GetTempPath();
            var moongladeAppDataPath = Path.Join(tPath, "moonglade", "App_Data");
            if (Directory.Exists(moongladeAppDataPath))
            {
                Directory.Delete(moongladeAppDataPath, true);
            }
            Directory.CreateDirectory(moongladeAppDataPath);

            var feedDirectoryPath = Path.Join($"{moongladeAppDataPath}", "feed");
            if (!Directory.Exists(feedDirectoryPath))
            {
                Directory.CreateDirectory(feedDirectoryPath);
            }

            CleanDataCache(moongladeAppDataPath);
            return moongladeAppDataPath;
        }
    }
}
