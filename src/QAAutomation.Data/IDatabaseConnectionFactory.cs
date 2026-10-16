using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace QAAutomation.Data
{
    /// <summary>
    /// Opens a database connection and returns it as System.Data.Common.DbConnection - never a
    /// concrete engine type. This is the one provider-aware seam in the project: everything above
    /// it (the repository, the resetter) works against DbConnection and so does not know or care
    /// whether it is talking to PostgreSQL or SQL Server.
    /// </summary>
    public interface IDatabaseConnectionFactory
    {
        /// <summary>
        /// Opens a new connection and returns it already open. The caller owns the connection and
        /// is responsible for disposing it.
        /// </summary>
        Task<DbConnection> OpenAsync(CancellationToken cancellationToken);
    }
}
