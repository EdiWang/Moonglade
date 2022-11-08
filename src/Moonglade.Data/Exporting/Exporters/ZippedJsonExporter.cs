using Moonglade.Data.Infrastructure;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;

namespace Moonglade.Data.Exporting.Exporters;

public class ZippedJsonExporter<T> : IExporter<T> where T : class
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

    public async Task<ExportResult> ExportData<TResult>(Expression<Func<T, TResult>> selector, CancellationToken ct)
    {
        var data = await _repository.SelectAsync(selector, ct);
        var result = await ToZippedJsonResult(data, ct);
        return result;
    }

    private async Task<ExportResult> ToZippedJsonResult<TE>(IEnumerable<TE> list, CancellationToken ct)
    {
        var tempId = Guid.NewGuid().ToString();
        string exportDirectory = ExportManager.CreateExportDirectory(_directory, tempId);
        foreach (var item in list)
        {
            var json = JsonSerializer.Serialize(item, MoongladeJsonSerializerOptions.Default);
            await SaveJsonToDirectory(json, exportDirectory, $"{Guid.NewGuid()}.json", ct);
        }

        var distPath = Path.Join(_directory, "export", $"{_fileNamePrefix}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.zip");
        ZipFile.CreateFromDirectory(exportDirectory, distPath);

        return new()
        {
            ExportFormat = ExportFormat.ZippedJsonFiles,
            FilePath = distPath
        };
    }

    private static async Task SaveJsonToDirectory(string json, string directory, string filename, CancellationToken ct)
    {
        var path = Path.Join(directory, filename);
        await File.WriteAllTextAsync(path, json, Encoding.UTF8, ct);
    }
}