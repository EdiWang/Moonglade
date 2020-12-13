using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moonglade.Data.Infrastructure;
using Newtonsoft.Json;

namespace Moonglade.DataPorting
{
    public class SingeJsonExporter<T> : IExporter<T>
    {
        private readonly IRepository<T> _repository;

        public SingeJsonExporter(IRepository<T> repository)
        {
            _repository = repository;
        }

        public async Task<ExportResult> ExportData<TResult>(Expression<Func<T, TResult>> selector)
        {
            var data = await _repository.SelectAsync(selector);
            var json = JsonConvert.SerializeObject(data, Formatting.Indented); //JsonSerializer.Serialize(data);

            return new()
            {
                ExportFormat = ExportFormat.SingleJsonFile,
                JsonContent = json
            };
        }
    }
}