using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using QAAutomation.Api.Contracts;
using QAAutomation.Core.Utilities;
using Refit;

namespace QAAutomation.Api.Clients
{
    /// <summary>
    /// The class step definitions actually inject.
    ///
    /// Its whole job is: make one request, read the transport details once, and hand back a
    /// plain record. It does not assert, it does not know what a scenario is trying to prove,
    /// and it holds no URL - the base address is configured in ApiRegistration from ApiOptions.
    ///
    /// Why a wrapper at all, when the Refit interface is already typed? Because a step is not
    /// allowed to touch HttpResponseHeaders, a Refit type or a JSON string
    /// (.kiro/steering/step-definitions.md). Reading a header and parsing an error body are
    /// transport concerns, so they happen here, once, instead of in every step that needs them.
    /// </summary>
    public class PagesClient
    {
        /// <summary>
        /// Ask the API for the four fields the suite uses and nothing else. A narrow request is
        /// cheaper for a production site, and a narrow contract has less surface to drift.
        /// </summary>
        private const string FieldSelection = "id,slug,title,link";

        /// <summary>Header carrying the total number of published pages.</summary>
        private const string TotalCountHeaderName = "X-WP-Total";

        /// <summary>Header carrying the total number of result pages at the requested page size.</summary>
        private const string TotalPagesHeaderName = "X-WP-TotalPages";

        private readonly IWordPressApi wordPressApi;

        public PagesClient(IWordPressApi wordPressApi)
        {
            if (wordPressApi == null)
            {
                throw new ArgumentNullException(nameof(wordPressApi));
            }

            this.wordPressApi = wordPressApi;
        }

        /// <summary>
        /// Lists pages at the requested page size. An out-of-range page size is passed straight
        /// through, because the refusal it produces is something the suite asserts on.
        /// </summary>
        public async Task<PageListResponse> ListPagesAsync(int perPage, CancellationToken cancellationToken)
        {
            using (IApiResponse<List<WordPressPageResponse>> response =
                await this.wordPressApi.GetPagesAsync(perPage, FieldSelection, cancellationToken))
            {
                PageListResponse listResponse = new PageListResponse();
                listResponse.StatusCode = ReadStatusCode(response);
                listResponse.TotalCountHeader = HeaderValueReader.ReadFirstValue(response.Headers, TotalCountHeaderName);
                listResponse.TotalPagesHeader = HeaderValueReader.ReadFirstValue(response.Headers, TotalPagesHeaderName);
                listResponse.Error = ReadError(response);

                // Empty rather than null on a refusal, so a step can count the pages without
                // first checking for null.
                if (response.Content == null)
                {
                    listResponse.Pages = new List<WordPressPageResponse>();
                }
                else
                {
                    listResponse.Pages = response.Content;
                }

                return listResponse;
            }
        }

        /// <summary>
        /// Finds the pages matching a slug. The API answers with a list, so this reports how
        /// many matched and hands back the first match.
        /// </summary>
        public async Task<PageLookupResponse> FindPagesBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                throw new ArgumentException("A page slug must be supplied.", nameof(slug));
            }

            using (IApiResponse<List<WordPressPageResponse>> response =
                await this.wordPressApi.GetPagesBySlugAsync(slug, FieldSelection, cancellationToken))
            {
                PageLookupResponse lookupResponse = new PageLookupResponse();
                lookupResponse.StatusCode = ReadStatusCode(response);
                lookupResponse.Error = ReadError(response);
                lookupResponse.MatchCount = 0;
                lookupResponse.Page = null;

                List<WordPressPageResponse> matchedPages = response.Content;
                if (matchedPages == null)
                {
                    return lookupResponse;
                }

                lookupResponse.MatchCount = matchedPages.Count;

                // An explicit loop with a break, not FirstOrDefault: take the first match and
                // stop. See .kiro/steering/code-style.md.
                foreach (WordPressPageResponse matchedPage in matchedPages)
                {
                    lookupResponse.Page = matchedPage;
                    break;
                }

                return lookupResponse;
            }
        }

        /// <summary>
        /// Reads one page by its identifier. A 404 comes back as data, not as an exception, so
        /// the missing-page scenario can assert on it.
        /// </summary>
        public async Task<PageLookupResponse> GetPageByIdAsync(int pageId, CancellationToken cancellationToken)
        {
            if (pageId <= 0)
            {
                throw new ArgumentException("A page identifier must be greater than zero.", nameof(pageId));
            }

            using (IApiResponse<WordPressPageResponse> response =
                await this.wordPressApi.GetPageAsync(pageId, FieldSelection, cancellationToken))
            {
                PageLookupResponse lookupResponse = new PageLookupResponse();
                lookupResponse.StatusCode = ReadStatusCode(response);
                lookupResponse.Error = ReadError(response);
                lookupResponse.Page = response.Content;

                if (response.Content == null)
                {
                    lookupResponse.MatchCount = 0;
                }
                else
                {
                    lookupResponse.MatchCount = 1;
                }

                return lookupResponse;
            }
        }

        /// <summary>
        /// Reads the status code the API answered with.
        ///
        /// Refit reports the status code as a value that can be missing, because a request can
        /// fail before any server answers it - a wrong host name, a timeout, no network. There
        /// is no status code to report in that case, so this fails with a plain message instead
        /// of inventing one and letting a step report a confusing mismatch.
        /// </summary>
        private static HttpStatusCode ReadStatusCode(IApiResponse response)
        {
            if (response.StatusCode == null)
            {
                throw new InvalidOperationException(
                    "The API did not answer, so there is no status code to report. Check the configured Api:BaseUrl and that the machine running the tests can reach it.");
            }

            return response.StatusCode.Value;
        }

        /// <summary>
        /// Turns a refusal into a typed record.
        ///
        /// Refit reports a failure through response.Error, whose declared type is the base
        /// exception type. The response body is only on ApiException, so it is cast - and the
        /// cast is checked, because Error is null on success and could be a transport failure
        /// (a DNS error, a timeout) that carries no body at all.
        ///
        /// A refusal body is not guaranteed to be JSON. The site sits behind a cache and an
        /// edge layer, so a 502 or 503 can arrive as an HTML error page. Parsing that would
        /// throw here, inside the client, and the step would never reach its status-code
        /// assertion - the output would show a JSON parsing error instead of "the site answered
        /// 503". So an unparseable body is reported as no typed error, and the status code the
        /// step asserts on is what names the problem.
        /// </summary>
        private static WordPressErrorResponse ReadError(IApiResponse response)
        {
            ApiException apiException = response.Error as ApiException;
            if (apiException == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(apiException.Content))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<WordPressErrorResponse>(apiException.Content);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
