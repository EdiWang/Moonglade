using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public class PingbackReadOnlySpec : Specification<PingbackEntity>
{
    public PingbackReadOnlySpec()
    {
        Query.AsNoTracking();
    }
}