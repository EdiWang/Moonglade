using System;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Hosting;
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
            try
            {
                var info = $"Moonglade Version {Utils.AppVersion}\n" +
                           $"Directory: {Environment.CurrentDirectory} \n" +
                           $"x64 Process: {Environment.Is64BitProcess} \n" +
                           $"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription} \n" +
                           $"User Name: {Environment.UserName}";
                WriteMessage(info);

                var tmp = CreateDataDirectories();
                AppDomain.CurrentDomain.SetData(Constants.DataDirectory, tmp);
                WriteMessage($"Using data directory '{tmp}'");

                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                Console.WriteLine("Error starting moonglade: " + ex.Message);
                throw;
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

                    //bool runsInDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
                    //if (!runsInDocker)
                    //{
                    //    // Because NLog may not be able to write files and find correct directory in a docker conatiner
                    //    // So only non-container environments are enabled for NLog
                    //    // Docker can still use Console log
                    //    webBuilder.UseNLog();
                    //}
                });

        private static string CreateDataDirectories()
        {
            void DeleteDataFile(string path)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
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

        private static void WriteMessage(string message)
        {
            Trace.WriteLine(message);
            Console.WriteLine(message);
        }
    }
}
