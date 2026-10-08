using System;
using System.Collections.ObjectModel;
using OpenQA.Selenium;
using QAAutomation.Core.Utilities;

namespace QAAutomation.UI.Components
{
    /// <summary>
    /// The header navigation bar that appears on every page of the site.
    ///
    /// LOCATOR NOTE - A DELIBERATE, DOCUMENTED EXCEPTION.
    /// .kiro/steering/ui-automation-selenium.md section 1 says data-testid is the default
    /// locator strategy. The system under test here is a third-party WordPress site: it has no
    /// data-testid attributes and we cannot add any, because we do not own the markup. So this
    /// file uses the most stable selectors that actually exist on the page. On KEYinfinity,
    /// where we do own the front end, data-testid becomes the primary strategy again and
    /// TestIdLocator (which ships unused in Core/Utilities) is how it is built. Any XPath added
    /// here must carry its own inline justification; there is none at the moment.
    ///
    /// WHY EVERY LOOKUP GOES THROUGH WaitUntilFirstVisible.
    /// The page contains TWO nav.site-header__navigation elements: a desktop copy and a mobile
    /// copy. At the 1920x1080 window this suite runs at, only one of them is displayed. A bare
    /// FindElement returns whichever comes first in the DOM, which may be the hidden one, and
    /// clicking a link inside a hidden element fails with ElementNotInteractableException. The
    /// shared wait helper's WaitUntilFirstVisible returns the first DISPLAYED match, which makes
    /// this deterministic.
    ///
    /// Methods are synchronous: Selenium's C# API is synchronous, so there is nothing to await
    /// and no *Async suffix anywhere. Nothing in this class asserts - it returns state and the
    /// step definition does the asserting.
    /// </summary>
    public class PrimaryNavigationComponent
    {
        private const string HeaderNavigationCssSelector = "nav.site-header__navigation";
        private const string LinkCssSelector = "a";

        private static readonly By HeaderNavigation = By.CssSelector(HeaderNavigationCssSelector);
        private static readonly By NavigationLink = By.CssSelector(LinkCssSelector);

        private readonly ElementWaits waits;
        private readonly SubscribeModalComponent subscribeModal;

        public PrimaryNavigationComponent(ElementWaits waits, SubscribeModalComponent subscribeModal)
        {
            this.waits = waits;
            this.subscribeModal = subscribeModal;
        }

        /// <summary>
        /// Blocks until a navigation bar is visible, so the page is genuinely usable before
        /// anything else happens to it.
        ///
        /// This exists alongside IsVisible() on purpose. IsVisible() returns state for a step
        /// to assert on; this one is only a wait, and a page object that wants the wait should
        /// say so rather than calling IsVisible() and throwing the answer away - C# gives no
        /// warning for a discarded return value, so that line reads as a no-op and is the kind
        /// of thing a reader deletes.
        /// </summary>
        public void WaitUntilVisible()
        {
            this.waits.WaitUntilFirstVisible(HeaderNavigation);
        }

        /// <summary>True when a navigation bar is on the page and visible to the visitor.</summary>
        public bool IsVisible()
        {
            IWebElement navigation = this.waits.WaitUntilFirstVisible(HeaderNavigation);

            return navigation.Displayed;
        }

        /// <summary>
        /// Clicks the navigation link whose visible text matches <paramref name="linkText"/>.
        ///
        /// The search is scoped to the visible navigation bar rather than the whole page, so a
        /// link with the same wording in the footer cannot be clicked by mistake.
        /// </summary>
        public void FollowLink(string linkText)
        {
            if (string.IsNullOrWhiteSpace(linkText))
            {
                throw new ArgumentException("A navigation link name is required.", nameof(linkText));
            }

            IWebElement navigation = this.waits.WaitUntilFirstVisible(HeaderNavigation);

            // The site opens a newsletter popup on its own ten seconds after every page load,
            // and while it is up it covers the navigation bar, so the click below is refused
            // with ElementClickInterceptedException. This closes it if it is up and costs
            // nothing if it is not. It goes here, after the navigation bar has been found and
            // just before the click, because waiting for the bar is the slow part and the popup
            // can open during that wait. See SubscribeModalComponent for the full story.
            this.subscribeModal.DismissIfShowing();

            ReadOnlyCollection<IWebElement> links = navigation.FindElements(NavigationLink);

            foreach (IWebElement link in links)
            {
                string actualLinkText = link.Text.Trim();
                bool sameWording = string.Equals(actualLinkText, linkText, StringComparison.OrdinalIgnoreCase);

                if (sameWording && link.Displayed)
                {
                    link.Click();
                    return;
                }
            }

            throw new NoSuchElementException(
                "The primary navigation has no visible link labelled '" + linkText + "'. "
                + "Link names are live website content, so check the site before changing this test. "
                + "For example the contact link is labelled 'Contact Sales', not 'Contact Us'.");
        }
    }
}
