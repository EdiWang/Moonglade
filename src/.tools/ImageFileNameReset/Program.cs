using System;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace ImageFileNameReset
{
    class Options
    {
        [Option('c', Required = true, HelpText = "Azure Blob Storage Connection String")]
        public string ConnectionString { get; set; }

        [Option('n', Required = true, HelpText = "Container Name")]
        public string ContainerName { get; set; }
    }

    class Program
    {
        public static Options Options { get; set; }

        static async Task Main(string[] args)
        {
            var parserResult = Parser.Default.ParseArguments<Options>(args);
            if (parserResult.Tag == ParserResultType.Parsed)
            {
                Options = ((Parsed<Options>)parserResult).Value;
                WriteMessage("Connecting to Azure...");

                var storageAccount = CloudStorageAccount.Parse(Options.ConnectionString);
                var blobClient = storageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference(Options.ContainerName);

                WriteMessage($"[OK] Connected to Azure Blob Storage '{storageAccount.Credentials.AccountName}' with container '{Options.ContainerName}'.", ConsoleColor.Green);

                WriteMessage("Getting image file list...");
                var blobs = await container.ListBlobsSegmentedAsync(null);
                var blobImages = (from item in blobs.Results
                                  where item.GetType() == typeof(CloudBlockBlob)
                                  select (CloudBlockBlob)item
                        into blob
                                  select new BlobImage(blob.Properties.LastModified, blob.Uri)
                                  {
                                      FileName = blob.Name
                                  })
                    .OrderByDescending(p => p.LastModified)
                    .ToList();

                WriteMessage($"Found {blobImages.Count} image file(s).", ConsoleColor.Yellow);

                var imagesToBeRenamed = blobImages.Where(b => !b.FileName.StartsWith("img-")).ToList();
                if (imagesToBeRenamed.Any())
                {
                    WriteMessage($"Found {imagesToBeRenamed.Count} image file(s) with non-standard names.", ConsoleColor.Yellow);
                }

                Console.ReadKey();
            }
        }

        public async Task RenameAsync(CloudBlobContainer container, string oldName, string newName)
        {
            try
            {
                var source = (CloudBlockBlob)await container.GetBlobReferenceFromServerAsync(oldName);
                var target = container.GetBlockBlobReference(newName);

                await target.StartCopyAsync(source);

                while (target.CopyState.Status == CopyStatus.Pending)
                    await Task.Delay(100);

                if (target.CopyState.Status != CopyStatus.Success)
                    throw new Exception("Rename failed: " + target.CopyState.Status);

                await source.DeleteAsync();
            }
            catch (Exception e)
            {
                WriteMessage(e.Message, ConsoleColor.Red);
            }
        }

        static void WriteMessage(string message, ConsoleColor color = ConsoleColor.White, bool resetColor = true)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            if (resetColor)
            {
                Console.ResetColor();
            }
        }
    }
}
