using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using QAAutomation.Core.Support;

namespace QAAutomation.Api.Clients
{
    /// <summary>
    /// Puts the scenario's correlation id on every outgoing request.
    ///
    /// A handler does this rather than each client method, so no client can forget it and no
    /// step ever has to think about a header. One id per scenario means a server-side log line
    /// and a UI failure screenshot can be matched to the same scenario run afterwards.
    ///
    /// Nothing here is logged. A request context can carry a token or a tenant identifier, and
    /// build artefacts are read more widely than people expect.
    ///
    /// ---------------------------------------------------------------------------------------
    /// PLACEHOLDER: KEYinfinity multi-tenant request context
    ///
    /// KEYinfinity is multi-tenant SaaS, and api-automation.md section 3 is explicit about how
    /// tenancy must be handled there:
    ///
    ///   * The tenant is a REQUIRED, EXPLICIT parameter on the client wrapper method. There is
    ///     no ambient default, no static "current tenant", and no silent fall back to a
    ///     configuration value when the caller leaves it out.
    ///   * The reason is the expensive bug class: tenant A seeing tenant B's data. A suite that
    ///     resolves tenancy implicitly cannot detect that, because every request would quietly
    ///     be correct.
    ///   * So the tenant is also ASSERTED on. Responses are checked for tenant-scoped
    ///     identifiers, not just for a 200.
    ///
    /// This handler is template-only scaffolding. When the suite points at KEYinfinity, the
    /// tenant does NOT get added here as an ambient header - it becomes an explicit parameter
    /// on PagesClient's equivalent methods, passed per request by the caller. Whether it
    /// travels as a header, a token claim or a path segment is still unconfirmed, and that
    /// answer changes this design.
    ///
    /// The WordPress site used by this template has no tenancy at all, so there is nothing to
    /// pass and nothing to assert here yet. That is why this is a comment and not code.
    /// ---------------------------------------------------------------------------------------
    /// </summary>
    public class RequestContextHandler : DelegatingHandler
    {
        private readonly ScenarioCorrelation scenarioCorrelation;

        public RequestContextHandler(ScenarioCorrelation scenarioCorrelation)
        {
            if (scenarioCorrelation == null)
            {
                throw new ArgumentNullException(nameof(scenarioCorrelation));
            }

            this.scenarioCorrelation = scenarioCorrelation;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // Remove first, then add: a retried request already carries the header, and adding
            // a second copy would send the value twice.
            request.Headers.Remove(ScenarioCorrelation.HeaderName);
            request.Headers.Add(ScenarioCorrelation.HeaderName, this.scenarioCorrelation.CorrelationIdText);

            return base.SendAsync(request, cancellationToken);
        }
    }
}
