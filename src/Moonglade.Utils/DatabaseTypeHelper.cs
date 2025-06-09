namespace Moonglade.Utils;

public class DatabaseTypeHelper
{
    public static DatabaseType DetermineDatabaseType(string connectionString)
    {
        var connStr = connectionString.ToLower();

        if (connStr.Contains("server=") && connStr.Contains("uid="))
            return DatabaseType.MySQL;
        if (connStr.Contains("server=") && connStr.Contains("user id="))
            return DatabaseType.MySQL;
        if (connStr.Contains("port=") && connStr.Contains("database=") && connStr.Contains("uid="))
            return DatabaseType.MySQL;

        if (connStr.Contains("host=") && connStr.Contains("username="))
            return DatabaseType.PostgreSQL;
        if (connStr.Contains("host=") && connStr.Contains("user id="))
            return DatabaseType.PostgreSQL;
        if (connStr.Contains("host=") && connStr.Contains("port=") && connStr.Contains("database="))
            return DatabaseType.PostgreSQL;

        if (connStr.Contains("data source=") || connStr.Contains("initial catalog="))
            return DatabaseType.SQLServer;
        if (connStr.Contains("server=") && connStr.Contains("database=") && connStr.Contains("user id="))
            return DatabaseType.SQLServer;
        if (connStr.Contains("integrated security="))
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
