using Moonglade.Data.Infrastructure;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;

namespace Moonglade.Data.Exporting.Exporters;

public class JsonExporter<T> : IExporter<T>
{
    private readonly IRepository<T> _repository;

    public JsonExporter(IRepository<T> repository)
    {
        _repository = repository;
    }

    public async Task<ExportResult> ExportData<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken)
    {
        var data = await _repository.SelectAsync(selector);
        var json = JsonSerializer.Serialize(data, MoongladeJsonSerializerOptions.Default);

        return new()
        {
            ExportFormat = ExportFormat.SingleJsonFile,
            Content = Encoding.UTF8.GetBytes(json)
        };
    }
}