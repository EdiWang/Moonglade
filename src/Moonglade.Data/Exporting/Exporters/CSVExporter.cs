using CsvHelper;
using Moonglade.Data.Infrastructure;
using System.Globalization;
using System.Linq.Expressions;

namespace Moonglade.Data.Exporting.Exporters;

public class CSVExporter<T> : IExporter<T> where T : class
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

    public async Task<ExportResult> ExportData<TResult>(Expression<Func<T, TResult>> selector, CancellationToken ct)
    {
        var data = await _repository.SelectAsync(selector, ct);
        var result = await ToCSVResult(data, ct);
        return result;
    }

    private async Task<ExportResult> ToCSVResult<TResult>(IEnumerable<TResult> data, CancellationToken ct)
    {
        var tempId = Guid.NewGuid().ToString();
        string exportDirectory = ExportManager.CreateExportDirectory(_directory, tempId);

        var distPath = Path.Join(exportDirectory, $"{_fileNamePrefix}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.csv");

        await using var writer = new StreamWriter(distPath);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        await csv.WriteRecordsAsync(data, ct);

        return new()
        {
            ExportFormat = ExportFormat.SingleCSVFile,
            FilePath = distPath
        };
    }
}