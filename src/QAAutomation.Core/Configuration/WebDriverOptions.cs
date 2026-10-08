namespace QAAutomation.Core.Configuration
{
    /// <summary>
    /// The WebDriver section of appsettings.json. A plain settings holder: properties only,
    /// no attributes and no behaviour, so it is readable without knowing any framework.
    /// </summary>
    public class WebDriverOptions
    {
        public string Browser { get; set; }

        public bool Headless { get; set; }

        public int WindowWidth { get; set; }

        public int WindowHeight { get; set; }

        /// <summary>How long a wait in ElementWaits may keep retrying before it gives up.</summary>
        public int ElementTimeoutSeconds { get; set; }

        public int PageLoadTimeoutSeconds { get; set; }
    }
}
