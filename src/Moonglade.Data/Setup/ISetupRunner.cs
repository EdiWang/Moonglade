namespace Moonglade.Data.Setup
{
    public interface ISetupRunner
    {
        void InitFirstRun();

        /// <summary>
        /// Check if the blog system is first run
        /// Either BlogConfiguration table does not exist or it has empty data is treated as first run.
        /// </summary>
        bool IsFirstRun();

        /// <summary>
        /// Execute SQL to build database schema
        /// </summary>
        void SetupDatabase();

        /// <summary>
        /// Clear all data in database but preserve tables schema
        /// </summary>
        void ClearData();

        void ResetDefaultConfiguration();

        bool TestDatabaseConnection();
    }
}
