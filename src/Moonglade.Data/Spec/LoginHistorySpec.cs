using Moonglade.Data.Entities;

namespace Moonglade.Data.Spec;

public sealed class LoginHistorySpec : Specification<LoginHistoryEntity>
{
    public LoginHistorySpec(int top)
    {
        Query.Skip(0).Take(top);
        Query.OrderByDescending(p => p.LoginTimeUtc);
    }
}