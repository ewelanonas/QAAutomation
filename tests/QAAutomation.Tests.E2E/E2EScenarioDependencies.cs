using Microsoft.Extensions.DependencyInjection;
using QAAutomation.Api;
using QAAutomation.Core;
using QAAutomation.Core.Configuration;
using QAAutomation.UI;
using Reqnroll.Microsoft.Extensions.DependencyInjection;

namespace QAAutomation.Tests.E2E
{
    /// <summary>
    /// The one place the end-to-end suite is wired up.
    ///
    /// Reqnroll calls this method to build the services for a scenario, then hands step classes
    /// and hooks whatever their constructors ask for. Nothing in this suite uses "new" to build
    /// a collaborator and nothing reaches into a static.
    ///
    /// THIS IS THE ONLY PROJECT THAT REGISTERS BOTH LAYERS. The API suite has no browser and
    /// the UI suite has no API client, which is how those two suites stay honest about what
    /// they are proving. An end-to-end scenario is the one case where crossing the boundary is
    /// the point, so both are registered here - and nowhere else.
    /// </summary>
    public static class E2EScenarioDependencies
    {
        [ScenarioDependencies]
        public static IServiceCollection CreateServices()
        {
            TestConfiguration testConfiguration = TestConfigurationLoader.Load();

            ServiceCollection services = new ServiceCollection();

            services.AddTestConfiguration(testConfiguration);

            // The browser, the one shared wait helper, failure-artefact capture and the
            // correlation id, all scoped so each scenario gets its own.
            //
            // This call also registers ScenarioCorrelation, which the API layer's outgoing
            // request handler needs. That is why it is not registered again below: one
            // registration, from the call that owns it. The API test project, which has no
            // browser support, has to register it itself.
            services.AddBrowserSupport();

            services.AddPageObjects();

            services.AddApiClients(testConfiguration.Api);

            services.AddScoped<CrossLayerContext>();

            return services;
        }
    }
}
