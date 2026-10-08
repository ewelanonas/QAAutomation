using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using QAAutomation.Api.Clients;
using QAAutomation.Api.Contracts;
using QAAutomation.Core.Configuration;
using Reqnroll;

namespace QAAutomation.Tests.Api.Steps
{
    /// <summary>
    /// Steps for Features/PageLookup.feature.
    ///
    /// SiteOptions is injected so the identifier that is known not to exist comes from
    /// configuration instead of being typed into a step. That is an environment fact, and an
    /// environment fact in a step body is the thing that makes a suite unusable somewhere else.
    /// </summary>
    [Binding]
    public class PageLookupSteps
    {
        private readonly PagesClient pagesClient;
        private readonly PageApiContext pageApiContext;
        private readonly SiteOptions siteOptions;

        public PageLookupSteps(PagesClient pagesClient, PageApiContext pageApiContext, SiteOptions siteOptions)
        {
            this.pagesClient = pagesClient;
            this.pageApiContext = pageApiContext;
            this.siteOptions = siteOptions;
        }

        [When("the page with the slug {string} is requested")]
        public async Task WhenThePageWithTheSlugIsRequested(string slug)
        {
            this.pageApiContext.LookupResponse = await this.pagesClient.FindPagesBySlugAsync(slug, CancellationToken.None);
        }

        [When("the page with an identifier that does not exist is requested")]
        public async Task WhenThePageWithAnIdentifierThatDoesNotExistIsRequested()
        {
            this.pageApiContext.LookupResponse = await this.pagesClient.GetPageByIdAsync(
                this.siteOptions.MissingPageId,
                CancellationToken.None);
        }

        [Then("the page lookup succeeds")]
        public void ThenThePageLookupSucceeds()
        {
            this.pageApiContext.LookupResponse.StatusCode.Should().Be(
                HttpStatusCode.OK,
                "a lookup for a page that is published is accepted");
        }

        [Then("exactly one page is returned")]
        public void ThenExactlyOnePageIsReturned()
        {
            // Exactly one is safe to assert here, and it is not a content count: a slug is
            // unique within the site, so a search by slug either finds one page or none. If
            // this ever returns two, the site has a genuine problem.
            this.pageApiContext.LookupResponse.MatchCount.Should().Be(
                1,
                "a slug identifies a single page, so a lookup by slug must not be ambiguous");
        }

        [Then("the returned page carries an identifier, a slug, a title and a link")]
        public void ThenTheReturnedPageCarriesAnIdentifierASlugATitleAndALink()
        {
            WordPressPageResponse returnedPage = this.pageApiContext.LookupResponse.Page;

            returnedPage.Should().NotBeNull(
                "the lookup reported a match, so the matching page must have come back with it");

            returnedPage.Id.Should().BeGreaterThan(
                0,
                "every page the API returns is identified by a positive number");

            returnedPage.Slug.Should().NotBeNullOrWhiteSpace(
                "the slug is the part of the public address that identifies the page");

            returnedPage.Link.Should().NotBeNullOrWhiteSpace(
                "a page found by lookup must be reachable, so it has to carry its public address");

            returnedPage.Title.Should().NotBeNull(
                "the title is requested in every call, so it must come back");

            returnedPage.Title.Rendered.Should().NotBeNullOrWhiteSpace(
                "a published page has a title a visitor can read");
        }

        [Then("the page lookup reports that the page was not found")]
        public void ThenThePageLookupReportsThatThePageWasNotFound()
        {
            this.pageApiContext.LookupResponse.StatusCode.Should().Be(
                HttpStatusCode.NotFound,
                "asking for a page that does not exist must be refused, not answered with something approximate");

            this.pageApiContext.LookupResponse.Page.Should().BeNull(
                "a refused lookup must not hand back a page");
        }
    }
}
