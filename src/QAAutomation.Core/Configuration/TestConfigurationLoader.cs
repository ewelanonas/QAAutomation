using System;
using System.Globalization;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace QAAutomation.Core.Configuration
{
    /// <summary>
    /// Loads and validates everything the suite reads from configuration.
    ///
    /// Two sources, in order: the appsettings.json that was copied next to the test assembly,
    /// then environment variables prefixed QAAUTOMATION_. The environment wins, which is how a
    /// secret or a per-environment override reaches the test process without ever being
    /// committed. Example: QAAUTOMATION_WebDriver__Headless=false runs the browser visibly.
    ///
    /// Values are read one at a time through GetSection and assigned explicitly. There is no
    /// Get&lt;T&gt;() call, because the Microsoft.Extensions.Configuration.Binder package is
    /// deliberately not pinned, and because explicit assignment is readable without knowing
    /// how binding works.
    /// </summary>
    public static class TestConfigurationLoader
    {
        private const string EnvironmentVariablePrefix = "QAAUTOMATION_";
        private const string SettingsFileName = "appsettings.json";

        private const int MinimumTimeoutSeconds = 1;
        private const int MaximumTimeoutSeconds = 300;
        private const int SmallestUsableWindowWidth = 320;
        private const int SmallestUsableWindowHeight = 480;
        private const int LargestAllowedPageSize = 100;

        public static TestConfiguration Load()
        {
            string settingsFilePath = Path.Combine(AppContext.BaseDirectory, SettingsFileName);

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddJsonFile(settingsFilePath, optional: false);
            builder.AddEnvironmentVariables(EnvironmentVariablePrefix);
            IConfigurationRoot configuration = builder.Build();

            TestConfiguration testConfiguration = new TestConfiguration();
            testConfiguration.WebDriver = ReadWebDriverOptions(configuration);
            testConfiguration.Api = ReadApiOptions(configuration);
            testConfiguration.Site = ReadSiteOptions(configuration);
            testConfiguration.Artefacts = ReadArtefactOptions(configuration);

            return testConfiguration;
        }

        private static WebDriverOptions ReadWebDriverOptions(IConfigurationRoot configuration)
        {
            WebDriverOptions options = new WebDriverOptions();
            options.Browser = ReadRequiredText(configuration, "WebDriver:Browser");
            options.Headless = ReadBoolean(configuration, "WebDriver:Headless");
            options.WindowWidth = ReadWholeNumber(configuration, "WebDriver:WindowWidth");
            options.WindowHeight = ReadWholeNumber(configuration, "WebDriver:WindowHeight");
            options.ElementTimeoutSeconds = ReadWholeNumber(configuration, "WebDriver:ElementTimeoutSeconds");
            options.PageLoadTimeoutSeconds = ReadWholeNumber(configuration, "WebDriver:PageLoadTimeoutSeconds");

            RequireAtLeast("WebDriver:WindowWidth", options.WindowWidth, SmallestUsableWindowWidth);
            RequireAtLeast("WebDriver:WindowHeight", options.WindowHeight, SmallestUsableWindowHeight);
            RequireSensibleTimeout("WebDriver:ElementTimeoutSeconds", options.ElementTimeoutSeconds);
            RequireSensibleTimeout("WebDriver:PageLoadTimeoutSeconds", options.PageLoadTimeoutSeconds);

            return options;
        }

        private static ApiOptions ReadApiOptions(IConfigurationRoot configuration)
        {
            ApiOptions options = new ApiOptions();
            options.BaseUrl = ReadRequiredText(configuration, "Api:BaseUrl");
            options.TimeoutSeconds = ReadWholeNumber(configuration, "Api:TimeoutSeconds");
            options.TenantId = ReadRequiredText(configuration, "Api:TenantId");
            options.ApiKey = ReadRequiredText(configuration, "Api:ApiKey");

            RequireSecureAbsoluteUrl("Api:BaseUrl", options.BaseUrl);
            RequireSensibleTimeout("Api:TimeoutSeconds", options.TimeoutSeconds);

            return options;
        }

        private static SiteOptions ReadSiteOptions(IConfigurationRoot configuration)
        {
            SiteOptions options = new SiteOptions();
            options.BaseUrl = ReadRequiredText(configuration, "Site:BaseUrl");
            options.HomePageTitle = ReadRequiredText(configuration, "Site:HomePageTitle");
            options.ContactPagePath = ReadRequiredText(configuration, "Site:ContactPagePath");
            options.KnownPageSlug = ReadRequiredText(configuration, "Site:KnownPageSlug");
            options.MissingPageId = ReadWholeNumber(configuration, "Site:MissingPageId");
            options.MinimumPageSize = ReadWholeNumber(configuration, "Site:MinimumPageSize");
            options.MaximumPageSize = ReadWholeNumber(configuration, "Site:MaximumPageSize");

            RequireSecureAbsoluteUrl("Site:BaseUrl", options.BaseUrl);
            RequireLeadingSlash("Site:ContactPagePath", options.ContactPagePath);
            RequireAtLeast("Site:MissingPageId", options.MissingPageId, 1);
            RequirePageSizeRange(options.MinimumPageSize, options.MaximumPageSize);

            return options;
        }

        private static ArtefactOptions ReadArtefactOptions(IConfigurationRoot configuration)
        {
            ArtefactOptions options = new ArtefactOptions();
            options.Directory = ReadRequiredText(configuration, "Artefacts:Directory");
            return options;
        }

        private static string ReadRequiredText(IConfigurationRoot configuration, string configurationKey)
        {
            string value = configuration.GetSection(configurationKey).Value;
            if (value == null)
            {
                throw TestConfigurationException.ForKey(configurationKey, "the value is missing", null);
            }

            string trimmedValue = value.Trim();
            if (trimmedValue.Length == 0)
            {
                throw TestConfigurationException.ForKey(configurationKey, "the value is empty", value);
            }

            return trimmedValue;
        }

        private static int ReadWholeNumber(IConfigurationRoot configuration, string configurationKey)
        {
            string value = ReadRequiredText(configuration, configurationKey);

            int parsedValue;
            bool parsed = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedValue);
            if (parsed == false)
            {
                throw TestConfigurationException.ForKey(configurationKey, "expected a whole number", value);
            }

            return parsedValue;
        }

        private static bool ReadBoolean(IConfigurationRoot configuration, string configurationKey)
        {
            string value = ReadRequiredText(configuration, configurationKey);

            bool parsedValue;
            bool parsed = bool.TryParse(value, out parsedValue);
            if (parsed == false)
            {
                throw TestConfigurationException.ForKey(configurationKey, "expected true or false", value);
            }

            return parsedValue;
        }

        private static void RequireSecureAbsoluteUrl(string configurationKey, string value)
        {
            Uri parsedUrl;
            bool parsed = Uri.TryCreate(value, UriKind.Absolute, out parsedUrl);
            if (parsed == false)
            {
                throw TestConfigurationException.ForKey(configurationKey, "expected an absolute URL", value);
            }

            if (parsedUrl.Scheme != Uri.UriSchemeHttps)
            {
                throw TestConfigurationException.ForKey(configurationKey, "expected an https URL", value);
            }
        }

        private static void RequireSensibleTimeout(string configurationKey, int seconds)
        {
            if (seconds < MinimumTimeoutSeconds || seconds > MaximumTimeoutSeconds)
            {
                string problem = "expected between " + MinimumTimeoutSeconds + " and "
                    + MaximumTimeoutSeconds + " seconds";
                throw TestConfigurationException.ForKey(configurationKey, problem, seconds.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void RequireAtLeast(string configurationKey, int actualValue, int smallestAllowedValue)
        {
            if (actualValue < smallestAllowedValue)
            {
                string problem = "expected at least " + smallestAllowedValue;
                throw TestConfigurationException.ForKey(configurationKey, problem, actualValue.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void RequireLeadingSlash(string configurationKey, string value)
        {
            if (value.StartsWith("/", StringComparison.Ordinal) == false)
            {
                throw TestConfigurationException.ForKey(configurationKey, "expected a path starting with /", value);
            }
        }

        private static void RequirePageSizeRange(int minimumPageSize, int maximumPageSize)
        {
            if (minimumPageSize < 1)
            {
                throw TestConfigurationException.ForKey("Site:MinimumPageSize", "expected at least 1",
                    minimumPageSize.ToString(CultureInfo.InvariantCulture));
            }

            if (maximumPageSize < minimumPageSize)
            {
                throw TestConfigurationException.ForKey("Site:MaximumPageSize", "expected at least Site:MinimumPageSize",
                    maximumPageSize.ToString(CultureInfo.InvariantCulture));
            }

            if (maximumPageSize > LargestAllowedPageSize)
            {
                string problem = "expected no more than " + LargestAllowedPageSize;
                throw TestConfigurationException.ForKey("Site:MaximumPageSize", problem,
                    maximumPageSize.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
