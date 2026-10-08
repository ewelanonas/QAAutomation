namespace QAAutomation.Core.Configuration
{
    /// <summary>
    /// The Artefacts section of appsettings.json. A plain settings holder: properties only.
    /// </summary>
    public class ArtefactOptions
    {
        /// <summary>
        /// Where failure artefacts are written. A relative value is resolved against the
        /// repository root so the folder is easy to find; an absolute value is used as-is.
        /// </summary>
        public string Directory { get; set; }
    }
}
