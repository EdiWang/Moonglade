using Moonglade.Auth;

namespace Moonglade.Web.Tests.TestSupport;

internal sealed class TestOwnerProfileReader : IOwnerProfileReader
{
    public OwnerProfile? Owner { get; set; } = new(Guid.NewGuid(), "owner@example.test");
    public int Reads { get; private set; }

    public Task<OwnerProfile?> ReadProfileAsync(CancellationToken ct = default)
    {
        Reads++;
        return Task.FromResult(Owner);
    }
}
