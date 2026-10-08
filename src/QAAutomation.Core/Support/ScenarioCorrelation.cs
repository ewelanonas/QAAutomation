using System;

namespace QAAutomation.Core.Support
{
    /// <summary>
    /// One identifier per scenario, so the evidence from different layers can be lined up.
    ///
    /// The same value goes onto every outgoing request as a header and into the location.txt
    /// file in the scenario's failure-artefact folder, which means a server-side log line and a
    /// screenshot can be matched to the same scenario run after the fact. It is not part of the
    /// folder name: that is the scenario title plus a timestamp, so the folders stay readable.
    /// Registered as a scoped service, so every class in one scenario sees the same identifier
    /// and the next scenario gets a new one.
    /// </summary>
    public class ScenarioCorrelation
    {
        public const string HeaderName = "X-Correlation-Id";

        public ScenarioCorrelation()
        {
            this.CorrelationId = Guid.NewGuid();
        }

        public Guid CorrelationId { get; private set; }

        public string CorrelationIdText
        {
            get { return this.CorrelationId.ToString("D"); }
        }
    }
}
