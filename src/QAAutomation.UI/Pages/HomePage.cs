using OpenQA.Selenium;
using QAAutomation.Core.Configuration;
using QAAutomation.UI.Components;

namespace QAAutomation.UI.Pages
{
    /// <summary>
    /// The public home page of the site under test.
    ///
    /// LOCATOR NOTE - A DELIBERATE, DOCUMENTED EXCEPTION.
    /// .kiro/steering/ui-automation-selenium.md section 1 makes data-testid the default locator
    /// strategy. This system under test is a third-party WordPress site: it carries no
    /// data-testid attributes and we cannot add any, because we do not own the markup. So the
    /// page objects in this project use the most stable selectors the site actually offers -
    /// here, the header navigation's own class name, reached through the shared component. On
    /// KEYinfinity, where we do own the front end, data-testid becomes the primary strategy
    /// again. Any XPath added to this file must carry its own inline justification; there is
    /// none at the moment.
    ///
    /// WHAT THIS PAGE OBJECT DELIBERATELY DOES NOT EXPOSE.
    /// The home page has a rotating hero carousel and a row of animated statistic counters.
    /// Neither is offered here, and no scenario may assert on them: their content depends on
    /// which slide happens to be showing and how far an animation has run, so an assertion on
    /// either would pass or fail on timing rather than on behaviour. The carousel also means
    /// the page has FOUR h1 elements, one per slide, so there is no "read the heading" method
    /// either - "the heading" is not a single thing on this page. The page title is stable, so
    /// that is what the scenarios check.
    ///
    /// Methods are synchronous because Selenium's C# API is synchronous. Nothing here asserts.
    /// </summary>
    public class HomePage
    {
        private readonly IWebDriver driver;
        private readonly SiteOptions siteOptions;
        private readonly PrimaryNavigationComponent navigation;
        private readonly LandedPage landedPage;

        public HomePage(
            IWebDriver driver,
            SiteOptions siteOptions,
            PrimaryNavigationComponent navigation,
            LandedPage landedPage)
        {
            this.driver = driver;
            this.siteOptions = siteOptions;
            this.navigation = navigation;
            this.landedPage = landedPage;
        }

        /// <summary>The header navigation bar, shared with every other page.</summary>
        public PrimaryNavigationComponent Navigation
        {
            get { return this.navigation; }
        }

        /// <summary>
        /// Opens the home page and waits until the navigation bar is visible, so the page is
        /// genuinely usable before any step does anything else.
        /// </summary>
        public void Open()
        {
            this.driver.Navigate().GoToUrl(this.siteOptions.BaseUrl);
            this.navigation.WaitUntilVisible();
        }

        /// <summary>The browser tab title of the home page.</summary>
        public string ReadTitle()
        {
            return this.driver.Title;
        }

        /// <summary>
        /// Clicks a link in the header navigation and hands back the page we landed on, so a
        /// step definition reads as a journey.
        ///
        /// LandedPage is injected rather than constructed here because it holds no state of its
        /// own - just the driver - so the scenario's single scoped instance is the right one to
        /// return, and a step definition can inject the same instance to read from.
        /// </summary>
        public LandedPage FollowNavigationLinkTo(string linkText)
        {
            this.navigation.FollowLink(linkText);

            return this.landedPage;
        }
    }
}
