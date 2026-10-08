using System;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using QAAutomation.Core.Configuration;
using QAAutomation.Core.Support;
using QAAutomation.Core.Utilities;

namespace QAAutomation.Core
{
    /// <summary>
    /// Registers Core's services. Each test project calls these from its own
    /// [ScenarioDependencies] factory, which is the one place in a suite where wiring lives.
    /// </summary>
    public static class CoreRegistration
    {
        /// <summary>
        /// Makes the four settings objects injectable.
        ///
        /// They are registered as plain singletons, not as IOptions&lt;T&gt;. That means a page
        /// object or a step class takes a constructor parameter of type SiteOptions and uses it
        /// directly - no .Value, no extra concept to learn. Singleton because configuration is
        /// read once per test run and never changes while it is running.
        /// </summary>
        public static IServiceCollection AddTestConfiguration(
            this IServiceCollection services,
            TestConfiguration testConfiguration)
        {
            services.AddSingleton(testConfiguration);
            services.AddSingleton(testConfiguration.WebDriver);
            services.AddSingleton(testConfiguration.Api);
            services.AddSingleton(testConfiguration.Site);
            services.AddSingleton(testConfiguration.Artefacts);

            return services;
        }

        /// <summary>
        /// Registers everything a browser-driving suite needs. The API suite does NOT call this,
        /// which is what keeps it from ever starting a browser by accident.
        ///
        /// Everything here is scoped, and Reqnroll opens one scope per scenario. So one scenario
        /// means one browser, one wait helper and one correlation id, shared by the hook and every
        /// step and page object in that scenario.
        /// </summary>
        public static IServiceCollection AddBrowserSupport(this IServiceCollection services)
        {
            services.AddScoped<ScenarioDriver>();
            services.AddScoped<ScenarioCorrelation>();
            services.AddScoped<FailureArtefacts>();

            // Resolved lazily, at the first step that needs it. By then the BeforeScenario hook
            // has already called Start(), so page objects can simply ask for an IWebDriver.
            services.AddScoped<IWebDriver>(provider => provider.GetRequiredService<ScenarioDriver>().Driver);

            services.AddScoped<ElementWaits>(provider => CreateElementWaits(provider));

            return services;
        }

        private static ElementWaits CreateElementWaits(IServiceProvider provider)
        {
            IWebDriver driver = provider.GetRequiredService<IWebDriver>();
            WebDriverOptions webDriverOptions = provider.GetRequiredService<WebDriverOptions>();
            TimeSpan timeout = TimeSpan.FromSeconds(webDriverOptions.ElementTimeoutSeconds);

            return new ElementWaits(driver, timeout);
        }
    }
}
