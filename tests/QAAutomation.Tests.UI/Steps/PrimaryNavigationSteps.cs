using AwesomeAssertions;
using QAAutomation.Core.Utilities;
using QAAutomation.UI.Pages;
using Reqnroll;

namespace QAAutomation.Tests.UI.Steps
{
    /// <summary>
    /// Steps for Features/PrimaryNavigation.feature.
    ///
    /// Plain void methods, because Selenium is synchronous. The Given that opens the home page
    /// lives in HomepageSteps and is reused verbatim - the same sentence must have exactly one
    /// binding in the suite, or Reqnroll reports an ambiguous step, and it is right to.
    ///
    /// ElementWaits is injected so the Then can wait for the new page's title before reading
    /// it. That is the one shared wait helper in the repository - the same one the page objects
    /// use - so there is still no WebDriverWait, no locator and no driver in this file. Without
    /// that wait the assertion would race the page load and fail roughly one run in twenty,
    /// which is the kind of failure nobody ever gets to the bottom of.
    /// </summary>
    [Binding]
    public class PrimaryNavigationSteps
    {
        private readonly HomePage homePage;
        private readonly LandedPage landedPage;
        private readonly SiteNavigationContext siteNavigationContext;
        private readonly ElementWaits waits;

        public PrimaryNavigationSteps(
            HomePage homePage,
            LandedPage landedPage,
            SiteNavigationContext siteNavigationContext,
            ElementWaits waits)
        {
            this.homePage = homePage;
            this.landedPage = landedPage;
            this.siteNavigationContext = siteNavigationContext;
            this.waits = waits;
        }

        [When("the visitor follows the {string} link in the primary navigation")]
        public void WhenTheVisitorFollowsTheLinkInThePrimaryNavigation(string linkText)
        {
            LandedPage openedPage = this.homePage.FollowNavigationLinkTo(linkText);

            // Recorded as evidence of where the journey ended up. It is also what a future
            // scenario would assert on if the requirement were about the address rather than
            // the page.
            this.siteNavigationContext.LandedUrl = openedPage.ReadCurrentUrl();
        }

        [Then("the browser page title mentions {string}")]
        public void ThenTheBrowserPageTitleMentions(string expectedTitlePart)
        {
            this.waits.WaitUntilTitleContains(expectedTitlePart);

            this.siteNavigationContext.PageTitle = this.landedPage.ReadTitle();

            // "Mentions" and not "is". These are marketing pages and their titles are written
            // for search engines, so they carry extra wording that changes without notice. The
            // page's own name is the part that identifies it.
            this.siteNavigationContext.PageTitle.Should().Contain(
                expectedTitlePart,
                "following a navigation link must land on the page the link names");
        }
    }
}
