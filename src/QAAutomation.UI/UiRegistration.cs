using Microsoft.Extensions.DependencyInjection;
using QAAutomation.UI.Components;
using QAAutomation.UI.Pages;

namespace QAAutomation.UI
{
    /// <summary>
    /// Makes the page objects injectable. Each browser-driving test project calls this from its
    /// own [ScenarioDependencies] factory.
    /// </summary>
    public static class UiRegistration
    {
        /// <summary>
        /// Registers every page object and page component as scoped.
        ///
        /// Scoped means one instance per scenario, which matches the browser: Reqnroll opens a
        /// scope per scenario, so a page object is always paired with that scenario's own driver
        /// and never shared with the scenario running next to it.
        /// </summary>
        public static IServiceCollection AddPageObjects(this IServiceCollection services)
        {
            services.AddScoped<SubscribeModalComponent>();
            services.AddScoped<PrimaryNavigationComponent>();
            services.AddScoped<HomePage>();
            services.AddScoped<LandedPage>();
            services.AddScoped<ContactUsPage>();

            return services;
        }
    }
}
