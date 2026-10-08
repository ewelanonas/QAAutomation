namespace QAAutomation.Core.Configuration
{
    /// <summary>
    /// The Site section of appsettings.json: facts about the system under test that scenarios
    /// need but must not hard-code. A plain settings holder: properties only, no behaviour.
    /// </summary>
    public class SiteOptions
    {
        public string BaseUrl { get; set; }

        /// <summary>The expected browser title of the home page.</summary>
        public string HomePageTitle { get; set; }

        /// <summary>Path of the enquiry page, including the leading slash.</summary>
        public string ContactPagePath { get; set; }

        /// <summary>A slug that is known to exist, used by the lookup and end-to-end scenarios.</summary>
        public string KnownPageSlug { get; set; }

        /// <summary>A page identifier that is known not to exist, used by the 404 scenario.</summary>
        public int MissingPageId { get; set; }

        /// <summary>Smallest page size the API documents as valid.</summary>
        public int MinimumPageSize { get; set; }

        /// <summary>Largest page size the API documents as valid.</summary>
        public int MaximumPageSize { get; set; }
    }
}
