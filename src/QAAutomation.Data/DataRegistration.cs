using System;
using Microsoft.Extensions.DependencyInjection;
using QAAutomation.Core.Configuration;

namespace QAAutomation.Data
{
    /// <summary>
    /// Registers the data-access layer. The API and E2E suites call this from their
    /// [ScenarioDependencies] factory, which is the one place a suite is wired up. The UI suite
    /// does NOT call it, which is what structurally stops a browser-only suite from ever opening
    /// a database connection.
    /// </summary>
    public static class DataRegistration
    {
        /// <summary>
        /// Wires the connection factory (chosen by provider), the repository and the resetter.
        ///
        /// Lifetimes, and why each is what it is:
        /// - the connection factory is a SINGLETON: it holds only the singleton DatabaseOptions and
        ///   opens a brand-new connection on every OpenAsync call, so it carries no per-scenario
        ///   state and is safe to share. Making it a singleton is also what stops the singleton
        ///   DatabaseResetter from capturing a shorter-lived service - a "captive dependency";
        /// - VatReturnRepository is SCOPED, so each scenario gets its own;
        /// - DatabaseResetter is a SINGLETON, so the Respawner it caches is built once per run.
        /// </summary>
        public static IServiceCollection AddDatabaseAccess(this IServiceCollection services, DatabaseOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            services.AddSingleton(options);

            RegisterConnectionFactory(services, options.Provider);

            services.AddScoped<VatReturnRepository>();
            services.AddSingleton<DatabaseResetter>();

            return services;
        }

        private static void RegisterConnectionFactory(IServiceCollection services, DatabaseProvider provider)
        {
            if (provider == DatabaseProvider.Postgres)
            {
                services.AddSingleton<IDatabaseConnectionFactory, NpgsqlConnectionFactory>();
                return;
            }

            if (provider == DatabaseProvider.SqlServer)
            {
                services.AddSingleton<IDatabaseConnectionFactory, SqlServerConnectionFactory>();
                return;
            }

            throw new InvalidOperationException("Unsupported database provider: " + provider + ".");
        }
    }
}
