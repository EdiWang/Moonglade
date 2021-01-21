using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Moonglade.Data.Infrastructure;
using Moonglade.Model;
using Newtonsoft.Json;

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
            string exportDirectory = ExportManager.CreateExportDirectory(tempId);
            foreach (var item in list)
            {
                var json = JsonConvert.SerializeObject(item, Formatting.Indented); // JsonSerializer.Serialize(item);
                await SaveJsonToDirectory(json, Path.Join(exportDirectory, tempId), $"{Guid.NewGuid()}.json");
            }

            var distPath = Path.Join(exportDirectory, $"{_fileNamePrefix}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.zip");
            ZipFile.CreateFromDirectory(Path.Join(exportDirectory, tempId), distPath);

            return new()
            {
                ExportFormat = ExportFormat.ZippedJsonFiles,
                FilePath = distPath
            };
        }

        private static async Task SaveJsonToDirectory(string json, string directory, string filename)
        {
            var path = Path.Join(directory, filename);
            await File.WriteAllTextAsync(path, json, Encoding.UTF8);
        }
    }
}
