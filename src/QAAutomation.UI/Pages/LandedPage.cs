using System;
using OpenQA.Selenium;

namespace QAAutomation.UI.Pages
{
    /// <summary>
    /// "Wherever the navigation took us."
    ///
    /// LOCATOR NOTE - A DELIBERATE, DOCUMENTED EXCEPTION.
    /// .kiro/steering/ui-automation-selenium.md section 1 makes data-testid the default locator
    /// strategy. This system under test is a third-party WordPress site with no data-testid
    /// attributes that we cannot change, so the page objects in this project use the most stable
    /// selectors the site actually offers. On KEYinfinity data-testid becomes primary again. Any
    /// XPath would need its own inline justification; this file uses no locators at all.
    ///
    /// WHY THIS PAGE OBJECT IS SO THIN.
    /// The site has a dozen marketing pages. Writing a page object for each one, just to read a
    /// title, would be a lot of files that prove nothing. This one stands in for all of them so
    /// a navigation journey still reads as a journey - home page, follow a link, check where we
    /// landed - without inventing a class per page. The moment a scenario needs to do something
    /// specific on a specific page, that page gets its own page object and this one stays as it
    /// is.
    ///
    /// Synchronous, like every page object here, because Selenium's C# API is synchronous. It
    /// asserts nothing; it returns values for a step definition to assert on.
    /// </summary>
    public class LandedPage
    {
        private readonly IWebDriver driver;

        public LandedPage(IWebDriver driver)
        {
            this.driver = driver;
        }

        /// <summary>
        /// Opens an address on the site under test directly, rather than arriving at it by
        /// following a link.
        ///
        /// This exists for the end-to-end suite, where the address comes from an API response
        /// instead of from a navigation link. The caller is responsible for checking that the
        /// address belongs to the site under test BEFORE calling this - see UrlHostChecker in
        /// QAAutomation.Core.Utilities. A page object navigates where it is told; deciding
        /// whether an address is trustworthy is not its job.
        ///
        /// No wait is done here. What counts as "loaded" depends on what the scenario is about
        /// to check, so the waiting belongs with that check - the end-to-end scenario waits for
        /// the title it is about to read.
        /// </summary>
        public void Open(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("An address must be supplied to open a page.", nameof(url));
            }

            this.driver.Navigate().GoToUrl(url);
        }

        /// <summary>The browser tab title of the page we are on.</summary>
        public string ReadTitle()
        {
            return this.driver.Title;
        }

        /// <summary>The address bar contents of the page we are on.</summary>
        public string ReadCurrentUrl()
        {
            return this.driver.Url;
        }
    }
}
