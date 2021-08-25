using Moonglade.Data.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Data.Porting.Exporters
{
    public class ZippedJsonExporter<T> : IExporter<T>
    {
        private readonly IRepository<T> _repository;
        private readonly string _fileNamePrefix;
        private readonly string _directory;

        public ZippedJsonExporter(IRepository<T> repository, string fileNamePrefix, string directory)
        {
            _repository = repository;
            _fileNamePrefix = fileNamePrefix;
            _directory = directory;
        }

        public async Task<ExportResult> ExportData<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken)
        {
            var data = await _repository.SelectAsync(selector);
            var result = await ToZippedJsonResult(data, cancellationToken);
            return result;
        }

        private async Task<ExportResult> ToZippedJsonResult<TE>(IEnumerable<TE> list, CancellationToken cancellationToken)
        {
            var tempId = Guid.NewGuid().ToString();
            string exportDirectory = ExportManager.CreateExportDirectory(_directory, tempId);
            foreach (var item in list)
            {
                var json = JsonSerializer.Serialize(item, MoongladeJsonSerializerOptions.Default);
                await SaveJsonToDirectory(json, exportDirectory, $"{Guid.NewGuid()}.json", cancellationToken);
            }

            var distPath = Path.Join(_directory, "export", $"{_fileNamePrefix}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.zip");
            ZipFile.CreateFromDirectory(exportDirectory, distPath);

            return new()
            {
                ExportFormat = ExportFormat.ZippedJsonFiles,
                FilePath = distPath
            };
        }

        private static async Task SaveJsonToDirectory(string json, string directory, string filename, CancellationToken cancellationToken)
        {
            var path = Path.Join(directory, filename);
            await File.WriteAllTextAsync(path, json, Encoding.UTF8, cancellationToken);
        }
    }
}
