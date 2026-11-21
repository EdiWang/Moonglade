namespace Moonglade.Data;

public interface IWidgetCollectionLock
{
    Task AcquireAsync(bool write, CancellationToken ct);
}
