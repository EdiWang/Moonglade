using System;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moonglade.Data.Infrastructure;
using Newtonsoft.Json;

namespace Moonglade.DataPorting.Exporters
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
            var json = JsonConvert.SerializeObject(data, Formatting.Indented); //JsonSerializer.Serialize(data);

            return new()
            {
                ExportFormat = ExportFormat.SingleJsonFile,
                Content = Encoding.UTF8.GetBytes(json)
            };
        }
    }
}