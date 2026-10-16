using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using QAAutomation.Core.Configuration;

namespace QAAutomation.Data
{
    /// <summary>
    /// Opens PostgreSQL connections using Npgsql. This is the only file that names Npgsql; the
    /// rest of the data layer sees the result as a plain DbConnection.
    /// </summary>
    public class NpgsqlConnectionFactory : IDatabaseConnectionFactory
    {
        /// <summary>
        /// Named in the error below so a run that forgot to supply the secret says exactly which
        /// environment variable is missing instead of failing with a vague connection error.
        /// </summary>
        private const string ConnectionStringEnvironmentVariable = "QAAUTOMATION_Database__ConnectionString";

        private readonly DatabaseOptions databaseOptions;

        public NpgsqlConnectionFactory(DatabaseOptions databaseOptions)
        {
            if (databaseOptions == null)
            {
                throw new ArgumentNullException(nameof(databaseOptions));
            }

            this.databaseOptions = databaseOptions;
        }

        public async Task<DbConnection> OpenAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(this.databaseOptions.ConnectionString))
            {
                throw new InvalidOperationException(
                    "The database connection string is not configured. It is a secret and must be supplied at run time "
                    + "through the environment variable " + ConnectionStringEnvironmentVariable + ".");
            }

            NpgsqlConnection connection = new NpgsqlConnection(this.databaseOptions.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }
}
