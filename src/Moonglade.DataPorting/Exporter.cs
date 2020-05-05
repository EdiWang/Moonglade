using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.DataPorting
{
    public interface IExporter<T>
    {
        Task<ExportResult> ExportData<TResult>(Expression<Func<T, TResult>> selector);
    }

    public class SingeJsonExporter<T> : IExporter<T>
    {
        private readonly IRepository<T> _repository;

        public SingeJsonExporter(IRepository<T> repository)
        {
            _repository = repository;
        }

        public async Task<ExportResult> ExportData<TResult>(Expression<Func<T, TResult>> selector)
        {
            var data = _repository.Select(selector);
            var json = JsonSerializer.Serialize(data);

            await Task.CompletedTask;
            return new ExportResult
            {
                ExportFormat = ExportFormat.SingleJsonFile,
                JsonContent = json
            };
        }
    }
}
