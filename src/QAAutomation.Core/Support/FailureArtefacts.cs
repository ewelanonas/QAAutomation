using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenQA.Selenium;
using QAAutomation.Core.Configuration;
using QAAutomation.Core.Utilities;

namespace QAAutomation.Core.Support
{
    /// <summary>
    /// Writes everything we will wish we had when a UI scenario fails.
    ///
    /// Selenium has no trace viewer, so a failed run leaves nothing behind unless we capture it
    /// ourselves. Four files, written into one folder per scenario:
    ///   screenshot.png    - what the page looked like at the moment of failure
    ///   page-source.html  - the DOM, for working out which element was missing
    ///   console.log       - browser console messages, where a JavaScript error shows up
    ///   location.txt      - the URL, the browser title and the correlation id
    ///
    /// SECURITY: page source and console logs are raw application output. They can contain
    /// tokens, session identifiers, email addresses and other personal data. Treat this folder as
    /// sensitive: never attach it to a public ticket, and never publish it unmasked from a build.
    ///
    /// Both files are written VERBATIM, with no masking pass. That is a recorded decision, not an
    /// oversight: against an anonymous public website there is nothing to mask, and a masking
    /// pass that guessed would either throw away the text a reader needs or protect only the
    /// shapes somebody thought of. The decision, and the condition that ends it - the first
    /// authenticated or multi-tenant target, where masking becomes a blocker rather than a
    /// follow-up - are written up in .kiro/steering/ui-automation-selenium.md section 6 rule 5.
    /// The driver's own log file is not captured either, for the reasons recorded in the same
    /// section.
    /// </summary>
    public class FailureArtefacts
    {
        private const string ScreenshotFileName = "screenshot.png";
        private const string PageSourceFileName = "page-source.html";
        private const string ConsoleLogFileName = "console.log";
        private const string LocationFileName = "location.txt";
        private const string CaptureProblemsFileName = "capture-problems.txt";

        private readonly ScenarioDriver scenarioDriver;
        private readonly ArtefactOptions artefactOptions;
        private readonly ScenarioCorrelation scenarioCorrelation;

        public FailureArtefacts(
            ScenarioDriver scenarioDriver,
            ArtefactOptions artefactOptions,
            ScenarioCorrelation scenarioCorrelation)
        {
            this.scenarioDriver = scenarioDriver;
            this.artefactOptions = artefactOptions;
            this.scenarioCorrelation = scenarioCorrelation;
        }

        /// <summary>
        /// Captures the four artefacts and returns the folder they were written to, or null if
        /// there was no browser to capture from.
        ///
        /// Each of the four writes is guarded on its own, so a partial capture is still a
        /// capture. The one most likely to throw is the screenshot - a browser that has already
        /// crashed, or a disk with no space - and the one most likely to explain that crash is
        /// location.txt, which is written last. Guarding the whole sequence once would mean
        /// losing the other three artefacts on the one run that most needed evidence.
        /// </summary>
        public string Capture(string suiteName, string scenarioTitle)
        {
            if (this.scenarioDriver.IsStarted == false)
            {
                return null;
            }

            IWebDriver driver = this.scenarioDriver.Driver;
            string folder = ArtefactPaths.ForScenario(this.artefactOptions.Directory, suiteName, scenarioTitle);

            try
            {
                SaveScreenshot(driver, folder);
            }
            catch (Exception screenshotFailure)
            {
                NoteProblem(folder, ScreenshotFileName, screenshotFailure);
            }

            try
            {
                SavePageSource(driver, folder);
            }
            catch (Exception pageSourceFailure)
            {
                NoteProblem(folder, PageSourceFileName, pageSourceFailure);
            }

            try
            {
                SaveConsoleLog(driver, folder);
            }
            catch (Exception consoleLogFailure)
            {
                NoteProblem(folder, ConsoleLogFileName, consoleLogFailure);
            }

            try
            {
                SaveLocation(driver, folder);
            }
            catch (Exception locationFailure)
            {
                NoteProblem(folder, LocationFileName, locationFailure);
            }

            return folder;
        }

        /// <summary>
        /// Records, in the scenario folder, that one artefact could not be written.
        ///
        /// The catches here and above are deliberately broad. This runs while a scenario is
        /// already failing, so anything thrown out of it replaces the real failure with a
        /// teardown error and the reader loses the actual story. A browser that has crashed
        /// throws a WebDriverException; a full or read-only disk throws an IOException; there
        /// is no shorter list worth writing out.
        /// </summary>
        private static void NoteProblem(string folder, string artefactFileName, Exception failure)
        {
            string noteText = artefactFileName + " could not be written: " + failure.Message + Environment.NewLine;

            try
            {
                string filePath = Path.Combine(folder, CaptureProblemsFileName);
                File.AppendAllText(filePath, noteText, Encoding.UTF8);
            }
            catch (Exception)
            {
                // The folder itself is unwritable, so there is nowhere left to record this and
                // nothing useful left to do. Swallowing it keeps the scenario's real failure
                // visible, which is the whole point of this class.
            }
        }

        private static void SaveScreenshot(IWebDriver driver, string folder)
        {
            ITakesScreenshot screenshotTaker = driver as ITakesScreenshot;
            if (screenshotTaker == null)
            {
                return;
            }

            Screenshot screenshot = screenshotTaker.GetScreenshot();
            string filePath = Path.Combine(folder, ScreenshotFileName);
            File.WriteAllBytes(filePath, screenshot.AsByteArray);
        }

        private static void SavePageSource(IWebDriver driver, string folder)
        {
            string filePath = Path.Combine(folder, PageSourceFileName);
            File.WriteAllText(filePath, driver.PageSource, Encoding.UTF8);
        }

        private static void SaveConsoleLog(IWebDriver driver, string folder)
        {
            StringBuilder logText = new StringBuilder();

            // Not every browser or grid exposes the console log. A missing log must never be the
            // reason a teardown blows up, so the read is guarded and the file still gets written.
            try
            {
                IEnumerable<LogEntry> entries = driver.Manage().Logs.GetLog(LogType.Browser);
                foreach (LogEntry entry in entries)
                {
                    logText.AppendLine(entry.Timestamp.ToString("o") + " [" + entry.Level + "] " + entry.Message);
                }
            }
            catch (WebDriverException readFailure)
            {
                logText.AppendLine("Browser console log could not be read: " + readFailure.Message);
            }

            string filePath = Path.Combine(folder, ConsoleLogFileName);
            File.WriteAllText(filePath, logText.ToString(), Encoding.UTF8);
        }

        private void SaveLocation(IWebDriver driver, string folder)
        {
            StringBuilder locationText = new StringBuilder();
            locationText.AppendLine("Url: " + driver.Url);
            locationText.AppendLine("Title: " + driver.Title);
            locationText.AppendLine("CorrelationId: " + this.scenarioCorrelation.CorrelationIdText);
            locationText.AppendLine("CapturedAtUtc: " + DateTime.UtcNow.ToString("o"));

            string filePath = Path.Combine(folder, LocationFileName);
            File.WriteAllText(filePath, locationText.ToString(), Encoding.UTF8);
        }
    }
}
