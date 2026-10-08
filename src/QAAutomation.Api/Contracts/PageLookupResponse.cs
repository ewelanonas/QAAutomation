using System.Net;

namespace QAAutomation.Api.Contracts
{
    /// <summary>
    /// Everything a step needs to know about a single-page lookup, in one plain object.
    ///
    /// Same reason as PageListResponse: the step definition stays free of transport detail.
    /// MatchCount is carried separately from Page because "exactly one page matched that slug"
    /// is a rule worth asserting, and it cannot be seen from Page alone.
    /// </summary>
    public record PageLookupResponse
    {
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>The matched page, or null when nothing matched or the request was refused.</summary>
        public WordPressPageResponse Page { get; set; }

        /// <summary>How many pages matched. Zero when nothing matched or the request was refused.</summary>
        public int MatchCount { get; set; }

        /// <summary>The refusal body, or null when the request was accepted.</summary>
        public WordPressErrorResponse Error { get; set; }
    }
}
