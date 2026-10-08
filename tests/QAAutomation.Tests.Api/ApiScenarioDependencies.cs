using Microsoft.Extensions.DependencyInjection;
using QAAutomation.Api;
using QAAutomation.Core;
using QAAutomation.Core.Configuration;
using QAAutomation.Core.Support;
using Reqnroll.Microsoft.Extensions.DependencyInjection;

namespace QAAutomation.Tests.Api
{
    /// <summary>
    /// The one place the API suite is wired up.
    ///
    /// Reqnroll calls this method to build the services for a scenario, then hands step classes
    /// what their constructors ask for. Nothing in this suite uses "new" to build a
    /// collaborator and nothing reaches into a static.
    ///
    /// Notice what is NOT here: no driver, no browser support, no page objects. The API suite
    /// cannot start a browser because nothing in it knows how to, which is suite separation
    /// made structural instead of conditional. There is no hooks class either - configuration
    /// is loaded here, and there is nothing to start or tear down.
    /// </summary>
    public static class ApiScenarioDependencies
    {
        [ScenarioDependencies]
        public static IServiceCollection CreateServices()
        {
            TestConfiguration testConfiguration = TestConfigurationLoader.Load();

            ServiceCollection services = new ServiceCollection();

            services.AddTestConfiguration(testConfiguration);
            services.AddApiClients(testConfiguration.Api);

            // One correlation id per scenario, put on every outgoing request by
            // RequestContextHandler so server-side logs can be matched to a scenario run.
            services.AddScoped<ScenarioCorrelation>();

            services.AddScoped<PageApiContext>();

            return services;
        }
    }
}
