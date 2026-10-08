using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using QAAutomation.Api.Clients;
using QAAutomation.Api.Contracts;
using QAAutomation.Core.Configuration;
using QAAutomation.Core.Utilities;
using QAAutomation.UI.Pages;
using Reqnroll;

namespace QAAutomation.Tests.E2E.Steps
{
    /// <summary>
    /// Steps for Features/PublishedPageRendersInBrowser.feature.
    ///
    /// WHY THESE ARE async Task AND THE UI SUITE'S ARE NOT.
    /// The Given makes an HTTP call, and HTTP in .NET is asynchronous, so the method has to be
    /// "async Task" and the call has to be "await"ed. The When drives the browser, and
    /// Selenium's C# API is synchronous, so that call sits plainly on its own line with no
    /// await - there is no Task to await. A step that mixes the two is async because of the
    /// HTTP half. See the sync/async table in .kiro/steering/step-definitions.md.
    ///
    /// Everything arrives through the constructor. There is no locator, no driver, no URL and
    /// no header name in this file: the client owns the transport and the page object owns the
    /// browser.
    /// </summary>
    [Binding]
    public class PublishedPageRendersInBrowserSteps
    {
        private readonly PagesClient pagesClient;
        private readonly LandedPage landedPage;
        private readonly CrossLayerContext crossLayerContext;
        private readonly SiteOptions siteOptions;
        private readonly ElementWaits waits;

        public PublishedPageRendersInBrowserSteps(
            PagesClient pagesClient,
            LandedPage landedPage,
            CrossLayerContext crossLayerContext,
            SiteOptions siteOptions,
            ElementWaits waits)
        {
            this.pagesClient = pagesClient;
            this.landedPage = landedPage;
            this.crossLayerContext = crossLayerContext;
            this.siteOptions = siteOptions;
            this.waits = waits;
        }

        /// <summary>
        /// Asks the content API which page to look at and what it should be called.
        ///
        /// The slug comes from Site:KnownPageSlug in appsettings.json rather than from a
        /// literal here, so pointing the scenario at a different page is a settings change.
        ///
        /// Nothing is asserted in a Given. A precondition that cannot be met is a set-up
        /// problem, not a test failure, so the guard below throws instead.
        /// </summary>
        [Given("the content API supplies the page with the configured known slug")]
        public async Task GivenTheContentApiSuppliesThePageWithTheConfiguredKnownSlug()
        {
            PageLookupResponse lookupResponse = await this.pagesClient.FindPagesBySlugAsync(
                this.siteOptions.KnownPageSlug,
                CancellationToken.None);

            if (lookupResponse.Page == null)
            {
                throw new InvalidOperationException(
                    "The content API returned no page for the slug '" + this.siteOptions.KnownPageSlug +
                    "'. The scenario cannot start. Check Site:KnownPageSlug in appsettings.json - the page may have been renamed or unpublished.");
            }

            this.crossLayerContext.ApiSlug = lookupResponse.Page.Slug;
            this.crossLayerContext.ApiTitle = lookupResponse.Page.Title.Rendered;
            this.crossLayerContext.ApiLink = lookupResponse.Page.Link;
        }

        /// <summary>
        /// Opens the address the API reported, after checking it is still an address on the
        /// site under test.
        /// </summary>
        [When("the visitor opens that page in the browser")]
        public void WhenTheVisitorOpensThatPageInTheBrowser()
        {
            // WHY THE ADDRESS IS CHECKED BEFORE IT IS OPENED.
            //
            // This address did not come from us. It came back in a response from a server, which
            // makes it untrusted input - exactly like a value typed into a form. If it ever
            // pointed at another host, this suite would drive a real browser to a site nobody
            // intended to visit, on a machine that may hold credentials. So the host is checked
            // first and the scenario stops with a plain message instead of navigating.
            //
            // A guard clause, not an assertion: a refusal to navigate is a safety stop, not a
            // statement about the behaviour under test.
            bool linkIsOnTheSiteUnderTest = UrlHostChecker.HasSameHost(
                this.siteOptions.BaseUrl,
                this.crossLayerContext.ApiLink);

            if (linkIsOnTheSiteUnderTest == false)
            {
                throw new InvalidOperationException(
                    "The content API returned an address on a different host, so the browser was not sent there. Expected the host of '" +
                    this.siteOptions.BaseUrl + "' but the API returned '" + this.crossLayerContext.ApiLink + "'.");
            }

            // Selenium is synchronous, so this is a plain call on its own line with no await.
            this.landedPage.Open(this.crossLayerContext.ApiLink);
        }

        /// <summary>
        /// Compares the title the browser shows against the title the API reported.
        /// </summary>
        [Then("the browser page title mentions the title the content API reported")]
        public void ThenTheBrowserPageTitleMentionsTheTitleTheContentApiReported()
        {
            // Wait for the title before reading it, through the one shared wait helper. Without
            // this the assertion races the page load and fails roughly one run in twenty, which
            // is the kind of failure nobody ever gets to the bottom of.
            this.waits.WaitUntilTitleContains(this.crossLayerContext.ApiTitle);

            this.crossLayerContext.BrowserTitle = this.landedPage.ReadTitle();

            // "MENTIONS", NOT "EQUALS" - and this is the contract, not a shortcut.
            //
            // The two titles are different things. The API reports the page's own name, which
            // for the configured page is "Technical". The browser shows the page's search-engine
            // title, which is "Technical - Landmark minimum recommended hardware requirements |
            // Landmark Systems". There is no uniform suffix to strip and no rule that turns one
            // into the other, because each page's search-engine title is written by hand.
            //
            // So equality is not assertable here. What the site really promises is that the page
            // the API named is the page the browser rendered, and the page's name appearing in
            // the browser title is how that shows. Case is ignored for the same reason: the
            // casing of marketing copy is an editorial choice, not behaviour.
            this.crossLayerContext.BrowserTitle.Should().ContainEquivalentOf(
                this.crossLayerContext.ApiTitle,
                "the page the content API named must be the page the browser renders, and the page's name is the part of the browser title that proves it");
        }
    }
}
