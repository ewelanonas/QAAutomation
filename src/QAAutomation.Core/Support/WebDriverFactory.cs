using System;
using System.Globalization;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using QAAutomation.Core.Configuration;

namespace QAAutomation.Core.Support
{
    /// <summary>
    /// Builds a browser from configuration. One place, so a browser setting is changed once.
    ///
    /// There is no driver download step and no driver package anywhere in this repository.
    /// Selenium Manager is built into Selenium 4.6 and above: it works out which driver the
    /// installed Chrome needs and fetches it on first run. Pinning a ChromeDriver version would
    /// bring back the exact version-drift problem Selenium Manager removed.
    /// </summary>
    public static class WebDriverFactory
    {
        private const string ChromeBrowserName = "chrome";

        public static IWebDriver Create(WebDriverOptions options)
        {
            if (string.Equals(options.Browser, ChromeBrowserName, StringComparison.OrdinalIgnoreCase) == false)
            {
                throw TestConfigurationException.ForKey(
                    "WebDriver:Browser",
                    "only 'chrome' is supported by this template",
                    options.Browser);
            }

            ChromeOptions chromeOptions = BuildChromeOptions(options);
            ChromeDriver driver = new ChromeDriver(chromeOptions);

            ApplyTimeouts(driver, options);

            return driver;
        }

        private static ChromeOptions BuildChromeOptions(WebDriverOptions options)
        {
            ChromeOptions chromeOptions = new ChromeOptions();

            if (options.Headless)
            {
                chromeOptions.AddArgument("--headless=new");
            }

            string windowSize = "--window-size="
                + options.WindowWidth.ToString(CultureInfo.InvariantCulture)
                + ","
                + options.WindowHeight.ToString(CultureInfo.InvariantCulture);
            chromeOptions.AddArgument(windowSize);
            chromeOptions.AddArgument("--disable-gpu");

            // Without this, driver.Manage().Logs.GetLog(LogType.Browser) comes back empty and the
            // console.log artefact is useless on a failure.
            chromeOptions.SetLoggingPreference(LogType.Browser, LogLevel.All);

            return chromeOptions;
        }

        private static void ApplyTimeouts(IWebDriver driver, WebDriverOptions options)
        {
            // Implicit wait stays at zero, deliberately. Mixing an implicit wait with the explicit
            // waits in ElementWaits produces unpredictable total timeouts, and "is this element
            // gone?" starts taking the implicit wait every single time.
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(options.PageLoadTimeoutSeconds);
        }
    }
}
