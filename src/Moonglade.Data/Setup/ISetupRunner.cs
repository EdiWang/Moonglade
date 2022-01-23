using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Moonglade.Data.Setup
{
    public interface ISetupRunner
    {
        /// <summary>
        /// Check if the blog system is first run
        /// Either BlogConfiguration table does not exist or it has empty data is treated as first run.
        /// </summary>
        bool IsFirstRun();

        /// <summary>
        /// Execute SQL to build database schema
        /// </summary>
        Task SetupDatabase(DatabaseFacade dbFacade);
    }
}
