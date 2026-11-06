namespace Moonglade.Configuration;

public sealed class ConfigurationWriteGate
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    public Task WaitAsync(CancellationToken ct) => _gate.WaitAsync(ct);
    public void Release() => _gate.Release();
}
