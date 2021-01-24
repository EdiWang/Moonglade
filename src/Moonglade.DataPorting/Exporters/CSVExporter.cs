using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CsvHelper;
using Moonglade.Data.Infrastructure;

namespace Moonglade.DataPorting.Exporters
{
    public class CSVExporter<T> : IExporter<T>
    {
        private readonly IRepository<T> _repository;
        private readonly string _fileNamePrefix;
        private readonly string _directory;

        public CSVExporter(IRepository<T> repository, string fileNamePrefix, string directory)
        {
            _repository = repository;
            _fileNamePrefix = fileNamePrefix;
            _directory = directory;
        }

        public async Task<ExportResult> ExportData<TResult>(Expression<Func<T, TResult>> selector)
        {
            var data = await _repository.SelectAsync(selector);
            var result = await ToCSVResult(data);
            return result;
        }

        private async Task<ExportResult> ToCSVResult<TResult>(IEnumerable<TResult> data)
        {
            var tempId = Guid.NewGuid().ToString();
            string exportDirectory = ExportManager.CreateExportDirectory(_directory, tempId);

            var distPath = Path.Join(exportDirectory, $"{_fileNamePrefix}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.csv");

            await using var writer = new StreamWriter(distPath);
            await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            await csv.WriteRecordsAsync(data);

            return new()
            {
                ExportFormat = ExportFormat.ZippedJsonFiles,
                FilePath = distPath
            };
        }
    }
}
