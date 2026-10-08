using System.Collections.Generic;
using System.Net;

namespace QAAutomation.Api.Contracts
{
    /// <summary>
    /// Everything a step needs to know about a page-list request, in one plain object.
    ///
    /// This type exists so that a step definition never touches HttpResponseHeaders, an
    /// HttpResponseMessage or a Refit type. The client reads the transport details once and
    /// hands back this. A step asks for StatusCode, Pages, a header value or Error, and nothing
    /// else. See .kiro/steering/step-definitions.md - a step may not handle headers or JSON.
    /// </summary>
    public record PageListResponse
    {
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// The pages that came back. Empty rather than null when the request was refused, so a
        /// step can count it without a null check first.
        /// </summary>
        public List<WordPressPageResponse> Pages { get; set; }

        /// <summary>
        /// The reported total number of published pages, still as text, or null when the header
        /// was absent. Kept as text on purpose: whether it is present and whether it parses are
        /// two separate things a test may want to say.
        /// </summary>
        public string TotalCountHeader { get; set; }

        /// <summary>
        /// The reported total number of result pages, still as text, or null when absent.
        /// </summary>
        public string TotalPagesHeader { get; set; }

        /// <summary>The refusal body, or null when the request was accepted.</summary>
        public WordPressErrorResponse Error { get; set; }
    }
}
