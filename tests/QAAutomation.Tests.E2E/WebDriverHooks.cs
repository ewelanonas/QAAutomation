using QAAutomation.Core.Support;
using Reqnroll;

namespace QAAutomation.Tests.E2E
{
    /// <summary>
    /// Starts a browser before every scenario and shuts it down afterwards.
    ///
    /// THIS FILE IS DELIBERATELY A COPY OF THE UI SUITE'S HOOKS, NOT A SHARED BASE CLASS.
    /// Two house rules put it here. project-structure.md forbids one test project referencing
    /// another, so it cannot be borrowed from tests\QAAutomation.Tests.UI. And Reqnroll only
    /// discovers a [Binding] class inside a test assembly, so moving it into Core would not
    /// work either - and if it somehow did, it would start a browser for the API suite, which
    /// is exactly the suite separation we are protecting. code-style.md settles the rest: test
    /// code is DAMP rather than DRY, and forty readable lines in two places beat an abstraction
    /// invented for a second occurrence. The only difference between the two copies is
    /// SuiteName.
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
        /// The folder name failure artefacts are grouped under, so end-to-end evidence does not
        /// land in the same place as the UI suite's.
        /// </summary>
        private const string SuiteName = "e2e";

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
        ///
        /// The browser is started even though this suite's first step talks to the API, not the
        /// browser. Starting it unconditionally keeps the hook free of an "if", and the whole
        /// point of an end-to-end scenario is that the browser is coming.
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
