using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using QAAutomation.Api.Clients;
using QAAutomation.Api.Contracts;
using QAAutomation.Core.Utilities;
using Reqnroll;

namespace QAAutomation.Tests.Api.Steps
{
    /// <summary>
    /// Steps for Features/PageListing.feature.
    ///
    /// Every body is one client call or one assertion. There is no URL here, no header name,
    /// no JSON and no status-code literal - the client owns the transport and HttpStatusCode
    /// gives the codes a name.
    /// </summary>
    [Binding]
    public class PageListingSteps
    {
        /// <summary>
        /// The machine-readable code the API returns when a query-string value is out of range.
        /// Named once here rather than spelled inline in two assertions.
        /// </summary>
        private const string InvalidParameterErrorCode = "rest_invalid_param";

        private readonly PagesClient pagesClient;
        private readonly PageApiContext pageApiContext;

        public PageListingSteps(PagesClient pagesClient, PageApiContext pageApiContext)
        {
            this.pagesClient = pagesClient;
            this.pageApiContext = pageApiContext;
        }

        [When("a page list is requested with a page size of {int}")]
        public async Task WhenAPageListIsRequestedWithAPageSizeOf(int pageSize)
        {
            this.pageApiContext.RequestedPageSize = pageSize;
            this.pageApiContext.ListResponse = await this.pagesClient.ListPagesAsync(pageSize, CancellationToken.None);
        }

        [Then("the page list request succeeds")]
        public void ThenThePageListRequestSucceeds()
        {
            this.pageApiContext.ListResponse.StatusCode.Should().Be(
                HttpStatusCode.OK,
                "a page list request made with a page size the API documents as valid is accepted");
        }

        [Then("every returned page carries an identifier, a slug, a title and a link")]
        public void ThenEveryReturnedPageCarriesAnIdentifierASlugATitleAndALink()
        {
            List<WordPressPageResponse> returnedPages = this.pageApiContext.ListResponse.Pages;

            returnedPages.Should().NotBeEmpty(
                "a published marketing site always has at least one page to list");

            // A plain foreach, not a LINQ chain: the loop can be stepped through and a
            // breakpoint can be put on any line of it. See .kiro/steering/code-style.md.
            foreach (WordPressPageResponse returnedPage in returnedPages)
            {
                returnedPage.Id.Should().BeGreaterThan(
                    0,
                    "every page the API returns is identified by a positive number");

                returnedPage.Slug.Should().NotBeNullOrWhiteSpace(
                    "the slug is the part of the public address that identifies the page");

                returnedPage.Link.Should().NotBeNullOrWhiteSpace(
                    "a listed page must be reachable, so it has to carry its public address");

                returnedPage.Title.Should().NotBeNull(
                    "the title is requested in every call, so it must come back");

                returnedPage.Title.Rendered.Should().NotBeNullOrWhiteSpace(
                    "a published page has a title a visitor can read");
            }
        }

        [Then("no more pages are returned than were requested")]
        public void ThenNoMorePagesAreReturnedThanWereRequested()
        {
            int returnedPageCount = this.pageApiContext.ListResponse.Pages.Count;

            // "At most", never "exactly". The rule the API promises is an upper bound: ask for
            // two and you get no more than two. Asserting equality would turn the last page of
            // results, or a site with fewer pages than the page size, into a failure that has
            // nothing to do with the behaviour under test.
            returnedPageCount.Should().BeLessThanOrEqualTo(
                this.pageApiContext.RequestedPageSize,
                "the API must never return more pages than the page size it was given");
        }

        [Then("the pagination headers report positive whole numbers")]
        public void ThenThePaginationHeadersReportPositiveWholeNumbers()
        {
            // WHY THIS ASSERTS "POSITIVE", NOT A NUMBER - the most important lesson in this
            // template.
            //
            // The site reports a total today of eighteen pages. The moment someone at the
            // company publishes or unpublishes a page, that number changes, and nobody tells
            // the test suite. A test that asserted eighteen would go red for no product reason,
            // somebody would "fix" it by editing the number, and after that happened twice
            // nobody would trust a red build again.
            //
            // So the rule asserted is the one the API actually promises and content cannot
            // break: both counts are reported, and both are positive whole numbers.
            int reportedTotalCount;
            bool totalCountIsPositive = PositiveIntegerParser.TryParsePositive(
                this.pageApiContext.ListResponse.TotalCountHeader,
                out reportedTotalCount);

            int reportedTotalPages;
            bool totalPagesIsPositive = PositiveIntegerParser.TryParsePositive(
                this.pageApiContext.ListResponse.TotalPagesHeader,
                out reportedTotalPages);

            // One concern - "the totals are reported sensibly" - so both checks run and the
            // failure message names both, instead of stopping at the first.
            using (new AssertionScope())
            {
                totalCountIsPositive.Should().BeTrue(
                    "the API must report how many pages exist as a positive whole number, whatever that number is today");

                totalPagesIsPositive.Should().BeTrue(
                    "the API must report how many result pages exist as a positive whole number, whatever that number is today");
            }
        }

        [Then("the page list request is rejected as an invalid parameter")]
        public void ThenThePageListRequestIsRejectedAsAnInvalidParameter()
        {
            this.pageApiContext.ListResponse.StatusCode.Should().Be(
                HttpStatusCode.BadRequest,
                "a page size outside the documented range is a bad request, not an empty result");

            this.pageApiContext.ListResponse.Error.Should().NotBeNull(
                "a refusal has to explain itself, so the API returns an error body");

            this.pageApiContext.ListResponse.Error.Code.Should().Be(
                InvalidParameterErrorCode,
                "the error code is the machine-readable part of the contract and does not change when the message is reworded");
        }
    }
}
