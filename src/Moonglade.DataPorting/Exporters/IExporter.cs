using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.DataPorting.Exporters
{
    public interface IExporter<T>
    {
        Task<ExportResult> ExportData<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken);
    }
}