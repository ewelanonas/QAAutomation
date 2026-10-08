namespace QAAutomation.Core.Configuration
{
    /// <summary>
    /// The Api section of appsettings.json. A plain settings holder: properties only,
    /// no attributes and no behaviour.
    /// </summary>
    public class ApiOptions
    {
        public string BaseUrl { get; set; }

        public int TimeoutSeconds { get; set; }

        /// <summary>
        /// Placeholder only. The WordPress API used by this template has no tenancy, so nothing
        /// reads this value yet. It exists to show where a real tenant id would come from:
        /// the environment variable QAAUTOMATION_Api__TenantId, never a committed file.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Placeholder only, and it must stay a placeholder in source control. A real key is
        /// supplied through the environment variable QAAUTOMATION_Api__ApiKey or the CI secret
        /// store, and is never logged.
        /// </summary>
        public string ApiKey { get; set; }
    }
}
