using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec;

public sealed class LoginHistorySpec : BaseSpecification<LoginHistoryEntity>
{
    public LoginHistorySpec(int top) : base(t => true)
    {
        ApplyPaging(0, top);
        ApplyOrderByDescending(p => p.LoginTimeUtc);
    }
}