using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Moonglade.DataPorting
{
    public interface IExporter<T>
    {
        Task<ExportResult> ExportData<TResult>(Expression<Func<T, TResult>> selector);
    }
}