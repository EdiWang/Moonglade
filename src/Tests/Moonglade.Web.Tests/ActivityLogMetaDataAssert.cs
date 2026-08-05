using Moonglade.ActivityLog;

namespace Moonglade.Web.Tests;

internal static class ActivityLogMetaDataAssert
{
    public static T Value<T>(CreateActivityLogCommand command, string name)
    {
        var metaData = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(command.MetaData);
        Assert.True(metaData.TryGetValue(name, out var value), $"Metadata key '{name}' was not found.");
        return Assert.IsType<T>(value);
    }
}
