using System.Linq.Expressions;

namespace Moonglade.Data.Porting.Exporters
{
    public interface IExporter<T>
    {
        Task<ExportResult> ExportData<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken);
    }
}