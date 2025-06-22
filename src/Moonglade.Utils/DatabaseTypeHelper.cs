public class DatabaseTypeHelper
{
    public static DatabaseType DetermineDatabaseType(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return DatabaseType.Unknown;

        var connStr = connectionString.ToLowerInvariant();

        if (connStr.Contains("server=") && (connStr.Contains("uid=") || connStr.Contains("user id=")) && !connStr.Contains("trusted_connection="))
            return DatabaseType.MySQL;
        if (connStr.Contains("port=") && connStr.Contains("database=") && (connStr.Contains("uid=") || connStr.Contains("user id=")))
            return DatabaseType.MySQL;

        if (connStr.Contains("host=") && (connStr.Contains("username=") || connStr.Contains("user id=")))
            return DatabaseType.PostgreSQL;
        if (connStr.Contains("host=") && connStr.Contains("port=") && connStr.Contains("database="))
            return DatabaseType.PostgreSQL;

        if (connStr.Contains("data source=") || connStr.Contains("initial catalog="))
            return DatabaseType.SQLServer;
        if (connStr.Contains("server=") && connStr.Contains("database=") &&
            (connStr.Contains("user id=") || connStr.Contains("trusted_connection=") || connStr.Contains("integrated security=")))
            return DatabaseType.SQLServer;
        if (connStr.Contains("trusted_connection=") || connStr.Contains("integrated security="))
            return DatabaseType.SQLServer;

        return DatabaseType.Unknown;
    }
}

public enum DatabaseType
{
    Unknown = 0,
    SQLServer,
    PostgreSQL,
    MySQL
}
