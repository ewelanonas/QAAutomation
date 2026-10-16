using System.Threading;
using System.Threading.Tasks;
using QAAutomation.Data;
using Reqnroll;

namespace QAAutomation.Tests.Api
{
    /// <summary>
    /// Resets the database after a scenario tagged @database.
    ///
    /// The "database" argument on [AfterScenario] is load-bearing: it tag-scopes the hook so it
    /// fires ONLY for @database scenarios. The ordinary API scenarios have no database behind
    /// them and would fail if this tried to open a connection, so it must never run for them.
    /// Reqnroll only instantiates a hook class when one of its hooks is actually due, so a run
    /// with no @database scenario never constructs this class and never resolves DatabaseResetter.
    /// </summary>
    [Binding]
    public class DatabaseResetHooks
    {
        private readonly DatabaseResetter databaseResetter;

        public DatabaseResetHooks(DatabaseResetter databaseResetter)
        {
            this.databaseResetter = databaseResetter;
        }

        /// <summary>
        /// Returns the database to a clean state so the next @database scenario starts fresh.
        /// </summary>
        [AfterScenario("database")]
        public async Task ResetDatabaseAsync()
        {
            await this.databaseResetter.ResetAsync(CancellationToken.None);
        }
    }
}
