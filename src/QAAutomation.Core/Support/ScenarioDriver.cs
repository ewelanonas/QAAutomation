using System;
using OpenQA.Selenium;
using QAAutomation.Core.Configuration;

namespace QAAutomation.Core.Support
{
    /// <summary>
    /// Owns the browser for one scenario: started in a BeforeScenario hook, closed in an
    /// AfterScenario hook. Registered as a scoped service, so the hook and every page object in
    /// the same scenario share this one instance, and the next scenario gets a fresh one.
    /// </summary>
    public class ScenarioDriver
    {
        private readonly WebDriverOptions webDriverOptions;
        private IWebDriver startedDriver;

        public ScenarioDriver(WebDriverOptions webDriverOptions)
        {
            this.webDriverOptions = webDriverOptions;
        }

        public bool IsStarted
        {
            get { return this.startedDriver != null; }
        }

        /// <summary>
        /// The live browser. Asking for this before Start() is a wiring mistake, not a test
        /// failure, so the message says what to fix rather than throwing a null reference.
        /// </summary>
        public IWebDriver Driver
        {
            get
            {
                if (this.startedDriver == null)
                {
                    throw new InvalidOperationException(
                        "The browser has not been started for this scenario. A [BeforeScenario] hook must call "
                        + "ScenarioDriver.Start() before any step uses a page object. Check that this test project "
                        + "has a WebDriverHooks class and that it registered AddBrowserSupport().");
                }

                return this.startedDriver;
            }
        }

        public void Start()
        {
            if (this.startedDriver != null)
            {
                return;
            }

            this.startedDriver = WebDriverFactory.Create(this.webDriverOptions);
        }

        /// <summary>
        /// Shuts the browser down. Quit() then Dispose(), never Close(): Close() shuts the window
        /// but leaves the chromedriver process running, and a few hundred of those will eventually
        /// bring a build agent to its knees.
        /// </summary>
        public void Quit()
        {
            if (this.startedDriver == null)
            {
                return;
            }

            this.startedDriver.Quit();
            this.startedDriver.Dispose();
            this.startedDriver = null;
        }
    }
}
