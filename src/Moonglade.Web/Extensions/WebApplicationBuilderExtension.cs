namespace Moonglade.Web.Extensions;

public static class WebApplicationBuilderExtension
{
    private const int MaxValueLength = 40;
    private const int KeyColumnWidth = 16;
    private const string Separator = "----------------------------------------------------------";

    public static void WriteParameterTable(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var appVersion = VersionHelper.AppVersion;
        var output = new StringBuilder();

        output.AppendLine(Separator);
        output.AppendLine($"Moonglade {appVersion} | .NET {Environment.Version}");
        output.AppendLine(Separator);

        var parameters = BuildParameterDictionary(builder.Configuration);

        foreach (var (key, value) in parameters)
        {
            var truncatedValue = TruncateValue(value, MaxValueLength);
            output.AppendLine($"{key,-KeyColumnWidth} | {truncatedValue,-MaxValueLength}");
        }

        output.AppendLine(Separator);

        Console.Write(output.ToString());
    }

    private static Dictionary<string, string> BuildParameterDictionary(IConfiguration configuration)
    {
        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "N/A";

        return new Dictionary<string, string>
        {
            { "Path", Environment.CurrentDirectory },
            { "System", VersionHelper.TryGetFullOSVersion() },
            { "User", Environment.UserName },
            { "Host", Environment.MachineName },
            { "URLs", configuration["Urls"] ?? "N/A" },
            { "Database", GetConnectionStringProvider(configuration) },
            { "Image storage", configuration["ImageStorage:Provider"] ?? "N/A" },
            { "Authentication", configuration["Authentication:Provider"] ?? "N/A" },
            { "Editor", configuration["Post:Editor"] ?? "N/A" },
            { "Environment", envName }
        };
    }

    private static string GetConnectionStringProvider(IConfiguration configuration)
    {
        var provider = configuration.GetConnectionString("DatabaseProvider");
        if (string.IsNullOrEmpty(provider))
        {
            // Fallback to check if there's a default connection string
            var defaultConnection = configuration.GetConnectionString("DefaultConnection");
            return string.IsNullOrEmpty(defaultConnection) ? "N/A" : "Configured";
        }
        return provider;
    }

    private static string TruncateValue(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value ?? "N/A";

        return value[..(maxLength - 3)] + "...";
    }
}
