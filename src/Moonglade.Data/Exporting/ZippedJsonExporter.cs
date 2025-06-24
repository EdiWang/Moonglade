using System.IO.Compression;
using System.Linq.Expressions;
using System.Text.Json;

namespace Moonglade.Data.Exporting;

public class ZippedJsonExporter<T>(MoongladeRepository<T> repository, string fileNamePrefix, string directory)
    where T : class
{
    public async Task<ExportResult> ExportData<TResult>(Expression<Func<T, TResult>> selector, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(selector);
        var data = await repository.SelectAsync(selector, ct);
        return await ToZippedJsonResult(data, ct);
    }

    private async Task<ExportResult> ToZippedJsonResult<TE>(IEnumerable<TE> list, CancellationToken ct)
    {
        var tempId = Guid.NewGuid().ToString();
        var exportDirectory = CreateExportDirectory(directory, tempId);

        // Serialize the entire list to a single file
        var jsonFileName = $"{fileNamePrefix}.json";
        var jsonFilePath = Path.Combine(exportDirectory, jsonFileName);

        await using (var fs = new FileStream(jsonFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(fs, list, MoongladeJsonSerializerOptions.Default, ct);
        }

        var zipDirectory = Path.Combine(directory, "export");
        Directory.CreateDirectory(zipDirectory);

        var zipFilePath = Path.Combine(zipDirectory, $"{fileNamePrefix}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.zip");
        ZipFile.CreateFromDirectory(exportDirectory, zipFilePath);

        // Clean up temp directory
        try { Directory.Delete(exportDirectory, true); } catch { /* ignore */ }

        return new ExportResult
        {
            FilePath = zipFilePath
        };
    }

    private static string CreateExportDirectory(string baseDirectory, string subDirName)
    {
        ArgumentNullException.ThrowIfNull(baseDirectory);

        var path = Path.Join(baseDirectory, "export", subDirName);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }

        Directory.CreateDirectory(path);
        return path;
    }
}
