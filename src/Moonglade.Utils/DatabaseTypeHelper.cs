namespace Moonglade.Utils;

public class DatabaseTypeHelper
{
    public static DatabaseType DetermineDatabaseType(string connectionString)
    {
        if (connectionString.Contains("Server=") && connectionString.Contains("Database=") && !connectionString.Contains("Uid=") ||
            connectionString.Contains("Initial Catalog") || connectionString.Contains("Data Source"))
        {
            return DatabaseType.SQLServer;
        }

        if (connectionString.Contains("Host=") && connectionString.Contains("Port="))
        {
            return DatabaseType.PostgreSQL;
        }

        if (connectionString.Contains("Server=") && connectionString.Contains("Uid="))
        {
            return DatabaseType.MySQL;
        }

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
