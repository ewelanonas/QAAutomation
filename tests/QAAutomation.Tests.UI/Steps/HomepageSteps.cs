using AwesomeAssertions;
using QAAutomation.Core.Configuration;
using QAAutomation.UI.Pages;
using Reqnroll;

namespace QAAutomation.Tests.UI.Steps
{
    /// <summary>
    /// Steps for Features/Homepage.feature.
    ///
    /// These are plain void methods, not async ones. Selenium's C# API is synchronous, so there
    /// is nothing to await and nothing to wrap in a Task - see the sync/async table in
    /// .kiro/steering/step-definitions.md.
    ///
    /// Everything arrives through the constructor. The page object knows how the page is built;
    /// this class only says what to do and what to expect. There is no locator, no driver and no
    /// wait on a specific element anywhere in this file, which is the whole point of the split.
    ///
    /// The Given opens the home page and asserts nothing. A failing precondition is a setup
    /// error, not a test failure, so it is left to throw.
    /// </summary>
    [Binding]
    public class HomepageSteps
    {
        private readonly HomePage homePage;
        private readonly SiteNavigationContext siteNavigationContext;
        private readonly SiteOptions siteOptions;

        public HomepageSteps(
            HomePage homePage,
            SiteNavigationContext siteNavigationContext,
            SiteOptions siteOptions)
        {
            this.homePage = homePage;
            this.siteNavigationContext = siteNavigationContext;
            this.siteOptions = siteOptions;
        }

        [Given("the visitor opens the Landmark home page")]
        public void GivenTheVisitorOpensTheLandmarkHomePage()
        {
            this.homePage.Open();
        }

        [Then("the browser page title is the expected home page title")]
        public void ThenTheBrowserPageTitleIsTheExpectedHomePageTitle()
        {
            // The expected title comes from Site:HomePageTitle in appsettings.json, not from a
            // literal here. It is live marketing copy, so when the company rewords it the fix
            // is one line of configuration rather than a change to a test.
            this.siteNavigationContext.PageTitle = this.homePage.ReadTitle();

            this.siteNavigationContext.PageTitle.Should().Be(
                this.siteOptions.HomePageTitle,
                "a visitor arriving from a search result reads the page title first, so it has to name the product");
        }

        [Then("the primary navigation is visible")]
        public void ThenThePrimaryNavigationIsVisible()
        {
            this.siteNavigationContext.NavigationVisible = this.homePage.Navigation.IsVisible();

            this.siteNavigationContext.NavigationVisible.Should().BeTrue(
                "the header navigation is the only way a visitor reaches the rest of the site");
        }
    }
}
