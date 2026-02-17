namespace Moonglade.BackgroundServices;

public class ScheduledPublishWakeUp
{
    private CancellationTokenSource _cts = new();

    public CancellationToken GetWakeToken()
    {
        lock (this)
        {
            if (_cts == null || _cts.IsCancellationRequested) _cts = new();
            return _cts.Token;
        }
    }

    public void WakeUp()
    {
        lock (this)
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }
    }
}
