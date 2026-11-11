using Moonglade.Auth;

namespace Moonglade.Web.Tests.TestSupport;

internal sealed class TestOwnerProfileReader : IOwnerProfileReader
{
    public OwnerProfile? Owner { get; set; } = new(Guid.NewGuid(), "owner@example.test");
    public OwnerPresentation Presentation { get; set; } = OwnerPresentation.Default;
    public Task<OwnerPresentation> ReadPresentationAsync(CancellationToken ct = default) => Task.FromResult(Presentation);

    public int Reads { get; private set; }

    public Task<OwnerProfile?> ReadProfileAsync(CancellationToken ct = default)
    {
        Reads++;
        return Task.FromResult(Owner);
    }
}
