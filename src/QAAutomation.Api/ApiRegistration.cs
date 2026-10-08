using System;
using Microsoft.Extensions.DependencyInjection;
using QAAutomation.Api.Clients;
using QAAutomation.Core.Configuration;
using Refit;

namespace QAAutomation.Api
{
    /// <summary>
    /// Registers the API layer. The API test suite calls this from its [ScenarioDependencies]
    /// factory, which is the one place in a suite where wiring lives.
    /// </summary>
    public static class ApiRegistration
    {
        /// <summary>
        /// Wires up the typed client, its HTTP pipeline and the wrapper that steps inject.
        ///
        /// Two points worth knowing:
        ///
        /// 1. AddRefitGeneratedClient, NOT AddRefitClient. AddRefitClient compiles, then fails
        ///    at RUN time asking for the separate Refit.Reflection package, which this
        ///    repository deliberately does not pin. The generated variant uses the client Refit
        ///    source-generated at build time, which is what we want anyway.
        /// 2. The HttpClient comes from IHttpClientFactory. No client news one up and no client
        ///    holds a static one, so socket handling and the handler pipeline stay in one place.
        /// </summary>
        public static IServiceCollection AddApiClients(this IServiceCollection services, ApiOptions apiOptions)
        {
            if (apiOptions == null)
            {
                throw new ArgumentNullException(nameof(apiOptions));
            }

            if (string.IsNullOrWhiteSpace(apiOptions.BaseUrl))
            {
                throw new ArgumentException("The API base URL must be configured.", nameof(apiOptions));
            }

            Uri baseAddress = BuildBaseAddress(apiOptions.BaseUrl);
            TimeSpan timeout = TimeSpan.FromSeconds(apiOptions.TimeoutSeconds);

            services.AddTransient<RequestContextHandler>();

            // The ConfigureHttpClient lambda is one of the three allowed lambdas in
            // code-style.md: the API accepts nothing else.
            services.AddRefitGeneratedClient<IWordPressApi>()
                .ConfigureHttpClient(client =>
                {
                    client.BaseAddress = baseAddress;
                    client.Timeout = timeout;
                })
                .AddHttpMessageHandler<RequestContextHandler>();

            services.AddScoped<PagesClient>();

            return services;
        }

        /// <summary>
        /// Builds the base address, removing a trailing slash if the configuration carries one.
        ///
        /// This matters more than it looks. The configured base URL has a path of its own
        /// (.../wp-json/wp/v2) and every request path on the Refit interface starts with a
        /// slash, so the two are joined as "base" + "/pages". A trailing slash on the base
        /// would make that ".../v2//pages", and a doubled slash is a 404 that looks like a
        /// product fault. Normalising here means the settings file can be written either way.
        /// </summary>
        private static Uri BuildBaseAddress(string configuredBaseUrl)
        {
            string normalisedBaseUrl = configuredBaseUrl.Trim();
            normalisedBaseUrl = normalisedBaseUrl.TrimEnd('/');

            return new Uri(normalisedBaseUrl, UriKind.Absolute);
        }
    }
}
