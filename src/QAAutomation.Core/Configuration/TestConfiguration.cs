namespace QAAutomation.Core.Configuration
{
    /// <summary>
    /// Everything the suite reads from configuration, in one place. A plain holder with no
    /// behaviour: TestConfigurationLoader fills it, and the DI registration hands the four
    /// options objects out individually.
    /// </summary>
    public class TestConfiguration
    {
        public WebDriverOptions WebDriver { get; set; }

        public ApiOptions Api { get; set; }

        public SiteOptions Site { get; set; }

        public ArtefactOptions Artefacts { get; set; }

        /// <summary>
        /// Database connection settings. The loader always fills this, but only the suites that
        /// opt in (API and E2E) pass it to AddDatabaseAccess, so Core itself stays free of any
        /// database concept and never registers a driver.
        /// </summary>
        public DatabaseOptions Database { get; set; }
    }
}
