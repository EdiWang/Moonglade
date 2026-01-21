using System.Security.Cryptography;
using System.Text;

namespace Moonglade.Setup;

/// <summary>
/// Utility class for generating SHA256 hashes of SQL migration scripts.
/// Use this during build/release process to generate expected hashes for script integrity validation.
/// </summary>
public static class ScriptHashGenerator
{
    /// <summary>
    /// Generates a SHA256 hash of the specified file content.
    /// </summary>
    /// <param name="filePath">Path to the SQL script file</param>
    /// <returns>Hex string representation of the SHA256 hash</returns>
    public static string GenerateHash(string filePath)
    {
        var content = File.ReadAllText(filePath);
        return GenerateHashFromContent(content);
    }

    /// <summary>
    /// Generates a SHA256 hash of the specified string content.
    /// </summary>
    /// <param name="content">The content to hash</param>
    /// <returns>Hex string representation of the SHA256 hash</returns>
    public static string GenerateHashFromContent(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Generates hashes for all migration scripts in the specified directory.
    /// </summary>
    /// <param name="migrationScriptsPath">Path to the MigrationScripts directory</param>
    /// <returns>Dictionary mapping provider key to hash value</returns>
    public static Dictionary<string, string> GenerateAllHashes(string migrationScriptsPath)
    {
        var hashes = new Dictionary<string, string>();
        var providers = new[] { "SqlServer", "MySql", "PostgreSql" };

        foreach (var provider in providers)
        {
            var scriptPath = Path.Combine(migrationScriptsPath, provider, "migration.sql");

            if (File.Exists(scriptPath))
            {
                hashes[provider] = GenerateHash(scriptPath);
            }
        }

        return hashes;
    }
}
