using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class PingbackReadOnlySpec : Specification<PingbackEntity>
{
    public PingbackReadOnlySpec()
    {
        Query.AsNoTracking();
    }
}