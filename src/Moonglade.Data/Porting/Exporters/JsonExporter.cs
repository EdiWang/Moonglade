using Moonglade.Data.Infrastructure;
using System;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Data.Porting.Exporters
{
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
}