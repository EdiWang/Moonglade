namespace Moonglade.BackgroundServices;

public class UpdateCheckerState
{
    private readonly Lock _lock = new();
    private string _newVersion;

    public string NewVersion
    {
        get
        {
            lock (_lock) return _newVersion;
        }
    }

    public void SetNewVersion(string version)
    {
        lock (_lock) _newVersion = version;
    }

    public bool HasNewVersion
    {
        get
        {
            lock (_lock) return !string.IsNullOrEmpty(_newVersion);
        }
    }
}
