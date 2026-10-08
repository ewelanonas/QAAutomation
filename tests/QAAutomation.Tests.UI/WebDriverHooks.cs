using QAAutomation.Core.Support;
using Reqnroll;

namespace QAAutomation.Tests.UI
{
    /// <summary>
    /// Starts a browser before every scenario and shuts it down afterwards.
    ///
    /// One browser per scenario is the isolation boundary. Selenium has no lighter-weight
    /// equivalent of a fresh session, and reusing one browser while clearing cookies between
    /// scenarios is not isolation - it leaves storage, in-memory page state and open dialogs
    /// behind, which is how one scenario comes to depend on the one before it.
    ///
    /// ScenarioDriver and FailureArtefacts are scoped, so the instances injected here are the
    /// same ones the step definitions and page objects get. ScenarioContext is injected only to
    /// read runner metadata - the scenario title and whether it failed.
    ///
    /// No hook here asserts anything, and nothing is logged beyond the artefact folder path:
    /// page source and console logs can contain tokens and personal data, so they are written
    /// to disk and never echoed into the test output.
    /// </summary>
    [Binding]
    public class WebDriverHooks
    {
        /// <summary>
        /// The folder name failure artefacts are grouped under, so UI evidence does not land in
        /// the same place as the end-to-end suite's.
        /// </summary>
        private const string SuiteName = "ui";

        private readonly ScenarioDriver scenarioDriver;
        private readonly FailureArtefacts failureArtefacts;
        private readonly ScenarioContext scenarioContext;

        public WebDriverHooks(
            ScenarioDriver scenarioDriver,
            FailureArtefacts failureArtefacts,
            ScenarioContext scenarioContext)
        {
            this.scenarioDriver = scenarioDriver;
            this.failureArtefacts = failureArtefacts;
            this.scenarioContext = scenarioContext;
        }

        /// <summary>
        /// Starts the browser. A low Order number because this is infrastructure: it has to be
        /// in place before anything a scenario can see.
        /// </summary>
        [BeforeScenario(Order = 10)]
        public void StartBrowser()
        {
            this.scenarioDriver.Start();
        }

        /// <summary>
        /// Captures evidence if the scenario failed, then always shuts the browser down.
        ///
        /// The order of the two matters: the screenshot has to be taken while the browser is
        /// still open. And the quit sits in a finally, so a capture that throws - a browser that
        /// has already crashed, a full disk - can never leave a chromedriver or Chrome process
        /// running. A few hundred of those will bring a build agent down.
        ///
        /// There is no early return anywhere in this method, for the same reason.
        /// </summary>
        [AfterScenario(Order = 100)]
        public void CaptureEvidenceThenQuitBrowser()
        {
            try
            {
                if (this.scenarioContext.TestError != null)
                {
                    this.failureArtefacts.Capture(SuiteName, this.scenarioContext.ScenarioInfo.Title);
                }
            }
            finally
            {
                this.scenarioDriver.Quit();
            }
        }
    }
}
