using Microsoft.Extensions.DependencyInjection;
using QAAutomation.Core;
using QAAutomation.Core.Configuration;
using QAAutomation.UI;
using Reqnroll.Microsoft.Extensions.DependencyInjection;

namespace QAAutomation.Tests.UI
{
    /// <summary>
    /// The one place the UI suite is wired up.
    ///
    /// Reqnroll calls this method to build the services for a scenario, then hands step classes
    /// and hooks whatever their constructors ask for. Nothing in this suite uses "new" to build
    /// a collaborator and nothing reaches into a static.
    ///
    /// Notice what is NOT here: no API client. The UI suite cannot make an API call because
    /// nothing in it knows how to, which is suite separation made structural rather than
    /// conditional. Cross-layer work belongs in the end-to-end suite, which registers both.
    /// </summary>
    public static class UiScenarioDependencies
    {
        [ScenarioDependencies]
        public static IServiceCollection CreateServices()
        {
            TestConfiguration testConfiguration = TestConfigurationLoader.Load();

            ServiceCollection services = new ServiceCollection();

            services.AddTestConfiguration(testConfiguration);

            // The browser, the one shared wait helper, failure-artefact capture and the
            // correlation id, all scoped so each scenario gets its own.
            services.AddBrowserSupport();

            services.AddPageObjects();

            services.AddScoped<SiteNavigationContext>();
            services.AddScoped<EnquiryFormContext>();

            return services;
        }
    }
}
