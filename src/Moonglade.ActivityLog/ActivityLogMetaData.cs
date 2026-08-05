namespace Moonglade.ActivityLog;

public static class ActivityLogMetaData
{
    public static IReadOnlyDictionary<string, object?> Create(params (string Name, object? Value)[] properties)
    {
        var metaData = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var (name, value) in properties)
        {
            metaData[name] = value;
        }

        return metaData;
    }
}
