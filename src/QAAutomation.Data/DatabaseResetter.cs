using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using QAAutomation.Core.Configuration;
using Respawn;

namespace QAAutomation.Data
{
    /// <summary>
    /// Resets the database to a clean state between scenarios, wrapping Respawn.
    ///
    /// Registered as a SINGLETON (see DataRegistration) so the Respawner is built once per run.
    /// Building it is the expensive part: CreateAsync reads the schema's foreign keys to work out
    /// a safe delete order, and that order does not change during a run. ResetAsync then replays
    /// that plan against a fresh connection each time it is called.
    /// </summary>
    public class DatabaseResetter
    {
        private readonly IDatabaseConnectionFactory connectionFactory;
        private readonly DatabaseOptions databaseOptions;

        // Built lazily on first reset and cached here, which is the whole reason this type is a
        // singleton. Not reassigned after it is set.
        private Respawner respawner;

        public DatabaseResetter(IDatabaseConnectionFactory connectionFactory, DatabaseOptions databaseOptions)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException(nameof(connectionFactory));
            }

            if (databaseOptions == null)
            {
                throw new ArgumentNullException(nameof(databaseOptions));
            }

            this.connectionFactory = connectionFactory;
            this.databaseOptions = databaseOptions;
        }

        /// <summary>
        /// Deletes all data while leaving the schema in place, so the next scenario starts clean.
        /// Builds the Respawner on first use and reuses it on every call after that.
        /// </summary>
        public async Task ResetAsync(CancellationToken cancellationToken)
        {
            await using (DbConnection connection = await this.connectionFactory.OpenAsync(cancellationToken))
            {
                if (this.respawner == null)
                {
                    this.respawner = await BuildRespawnerAsync(connection);
                }

                await this.respawner.ResetAsync(connection);
            }
        }

        private async Task<Respawner> BuildRespawnerAsync(DbConnection connection)
        {
            // DbAdapter is init-only, so it is set in the object initializer rather than assigned
            // afterwards.
            RespawnerOptions respawnerOptions = new RespawnerOptions
            {
                DbAdapter = ChooseDbAdapter(this.databaseOptions.Provider)
            };

            return await Respawner.CreateAsync(connection, respawnerOptions);
        }

        private static IDbAdapter ChooseDbAdapter(DatabaseProvider provider)
        {
            if (provider == DatabaseProvider.Postgres)
            {
                return DbAdapter.Postgres;
            }

            if (provider == DatabaseProvider.SqlServer)
            {
                return DbAdapter.SqlServer;
            }

            throw new InvalidOperationException("Unsupported database provider: " + provider + ".");
        }
    }
}
