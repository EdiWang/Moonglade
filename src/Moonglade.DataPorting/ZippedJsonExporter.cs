using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Moonglade.Data.Infrastructure;
using Moonglade.Model;

namespace Moonglade.DataPorting
{
    public class ZippedJsonExporter<T> : IExporter<T>
    {
        private readonly IRepository<T> _repository;
        private readonly string _fileNamePrefix;

        public ZippedJsonExporter(IRepository<T> repository, string fileNamePrefix)
        {
            _repository = repository;
            _fileNamePrefix = fileNamePrefix;
        }

        public async Task<ExportResult> ExportData<TResult>(Expression<Func<T, TResult>> selector)
        {
            var data = await _repository.SelectAsync(selector);
            var result = await ToZippedJsonResult(data);
            return result;
        }

        private async Task<ExportResult> ToZippedJsonResult<TE>(IEnumerable<TE> list)
        {
            var tempId = Guid.NewGuid().ToString();
            string exportDirectory = CreateExportDirectory(tempId);
            foreach (var item in list)
            {
                var json = JsonSerializer.Serialize(item);
                await SaveJsonToDirectory(json, Path.Join(exportDirectory, tempId), $"{Guid.NewGuid()}.json");
            }

            var distPath = Path.Join(exportDirectory, $"{_fileNamePrefix}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.zip");
            ZipFile.CreateFromDirectory(Path.Join(exportDirectory, tempId), distPath);

            return new ExportResult
            {
                ExportFormat = ExportFormat.ZippedJsonFiles,
                ZipFilePath = distPath
            };
        }

        private static async Task SaveJsonToDirectory(string json, string directory, string filename)
        {
            var path = Path.Join(directory, filename);
            await File.WriteAllTextAsync(path, json, Encoding.UTF8);
        }

        private static string CreateExportDirectory(string subDirName)
        {
            var dataDir = AppDomain.CurrentDomain.GetData(Constants.DataDirectory)?.ToString();
            if (null != dataDir)
            {
                var path = Path.Join(dataDir, "export", subDirName);
                if (Directory.Exists(path))
                {
                    Directory.Delete(path);
                }

                Directory.CreateDirectory(path);
                return Path.Join(dataDir, "export");
            }

            return null;
        }
    }
}
