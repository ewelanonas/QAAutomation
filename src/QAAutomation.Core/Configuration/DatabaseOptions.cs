namespace QAAutomation.Core.Configuration
{
    /// <summary>
    /// Which database engine the suite talks to. The connection factory in QAAutomation.Data
    /// reads this to decide which concrete driver to open, so the rest of the data layer never
    /// names an engine.
    /// </summary>
    public enum DatabaseProvider
    {
        /// <summary>PostgreSQL, driven by Npgsql. KEYinfinity's documented primary engine.</summary>
        Postgres,

        /// <summary>Microsoft SQL Server, driven by Microsoft.Data.SqlClient. Added for portability.</summary>
        SqlServer
    }

    /// <summary>
    /// The Database section of appsettings.json. A plain settings holder: properties only,
    /// no attributes and no behaviour. Lives in Core so no database package leaks into Core -
    /// only QAAutomation.Data references a driver.
    /// </summary>
    public class DatabaseOptions
    {
        /// <summary>
        /// Which engine to open. Defaults to Postgres in the committed settings. SqlServer is
        /// selected per environment by QAAUTOMATION_Database__Provider.
        /// </summary>
        public DatabaseProvider Provider { get; set; }

        /// <summary>
        /// SECRET - and it must stay a placeholder in source control. The committed appsettings.json
        /// carries an empty string; the real connection string is supplied at run time through the
        /// environment variable QAAUTOMATION_Database__ConnectionString or the CI secret store, is
        /// never committed and is never logged. The connection factory validates presence and names
        /// that env var in its error when the value is blank.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// How long a single database command may run before it is abandoned, in seconds.
        /// </summary>
        public int CommandTimeoutSeconds { get; set; }
    }
}
