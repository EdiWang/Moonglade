using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Moonglade.Web.Extensions;

public static class MoongladeHealthCheckOptions
{
    public const string LivenessTag = "live";
    public const string ReadinessTag = "ready";

    public static HealthCheckOptions CreateLivenessOptions() =>
        CreateOptions(registration => registration.Tags.Contains(LivenessTag));

    public static HealthCheckOptions CreateReadinessOptions() =>
        CreateOptions(registration => registration.Tags.Contains(ReadinessTag));

    private static HealthCheckOptions CreateOptions(Func<HealthCheckRegistration, bool> predicate) =>
        new()
        {
            Predicate = predicate,
            ResponseWriter = PingEndpoint.WriteResponse,
            AllowCachingResponses = false
        };
}
