using System.Collections.ObjectModel;
using OpenQA.Selenium;
using QAAutomation.Core.Utilities;

namespace QAAutomation.UI.Components
{
    /// <summary>
    /// The newsletter sign-up popup the site shows on its own, ten seconds after a page loads.
    ///
    /// WHY THIS CLASS EXISTS. The site's theme script schedules the popup with a ten-second
    /// timer on every page load. When it opens it covers the whole window, so ANY click made
    /// later than about ten seconds after the page loaded is refused with
    /// ElementClickInterceptedException - "Other element would receive the click:
    /// div class=modal-overlay is-visible". That is not a product fault and it is not a fault
    /// in the scenario either; it is a marketing popup racing the test. It is also intermittent
    /// by nature: a fast page is clicked before the timer fires and a slow one is not, so the
    /// same scenario passes on a quiet machine and fails on a busy one.
    ///
    /// WHY NOT JUST WAIT FOR THE POPUP AND CLOSE IT. Because most of the time it is not there.
    /// Waiting for something that will never appear burns the full element timeout on every
    /// single click. So this class LOOKS, and only waits once it knows there is something to
    /// wait for. The look is a plain FindElements call, which returns an empty list rather than
    /// throwing when nothing matches.
    ///
    /// WHY CLOSING IT ONCE IS ENOUGH. The site's own close handler writes a timestamp into the
    /// browser's local storage and will not show the popup again for an hour. Each scenario
    /// gets a fresh browser, so the popup appears at most once per scenario - on the first page
    /// slow enough for the timer to beat us to it.
    ///
    /// LOCATOR NOTE - A DELIBERATE, DOCUMENTED EXCEPTION.
    /// .kiro/steering/ui-automation-selenium.md section 1 says data-testid is the default
    /// locator strategy. This is a third-party WordPress site with no data-testid attributes
    /// and we cannot add any, so the selectors below are the theme's own class names, which are
    /// the most stable thing available. On KEYinfinity data-testid becomes primary again.
    ///
    /// Methods are synchronous, because Selenium's C# API is synchronous. Nothing here asserts.
    /// </summary>
    public class SubscribeModalComponent
    {
        // The theme adds "is-visible" to the overlay when it opens it, so this selector matches
        // only an OPEN popup.
        private const string OpenOverlayCssSelector = "div.modal-overlay.is-visible";

        // Without "is-visible" the overlay is the closed, invisible shell that sits in the page
        // from the start. This is what we wait to disappear after closing.
        private const string OverlayCssSelector = "div.modal-overlay";

        // Scoped to the open overlay so this can never match a close button somewhere else.
        private const string CloseButtonCssSelector =
            "div.modal-overlay.is-visible .modal-content--close-button";

        private static readonly By OpenOverlay = By.CssSelector(OpenOverlayCssSelector);
        private static readonly By Overlay = By.CssSelector(OverlayCssSelector);
        private static readonly By CloseButton = By.CssSelector(CloseButtonCssSelector);

        private readonly IWebDriver driver;
        private readonly ElementWaits waits;

        public SubscribeModalComponent(IWebDriver driver, ElementWaits waits)
        {
            this.driver = driver;
            this.waits = waits;
        }

        /// <summary>
        /// True when the popup is open and covering the page right now. This is a look, not a
        /// wait: it answers about this instant and never blocks.
        /// </summary>
        public bool IsShowing()
        {
            IWebElement openOverlay = this.FindFirstDisplayed(OpenOverlay);

            return openOverlay != null;
        }

        /// <summary>
        /// Closes the popup if it happens to be open, and does nothing at all if it is not.
        ///
        /// Call this immediately before a click. It is deliberately not called from a hook: a
        /// hook runs before the first page is even open, which is the one moment the popup can
        /// never be up.
        /// </summary>
        public void DismissIfShowing()
        {
            if (this.IsShowing() == false)
            {
                return;
            }

            // Now that we know it is there, the shared wait helper is the right tool. The
            // overlay fades in, so the close button can be on the page a moment before it can
            // actually be clicked.
            IWebElement closeButton = this.waits.WaitUntilClickable(CloseButton);
            closeButton.Click();

            // Do not return until the page is clear again, or the very next click would be
            // intercepted by the popup on its way out.
            this.waits.WaitUntilGone(Overlay);
        }

        private IWebElement FindFirstDisplayed(By locator)
        {
            // FindElements returns an empty list when nothing matches, where FindElement would
            // throw. That is exactly what "is it there?" needs.
            ReadOnlyCollection<IWebElement> matches = this.driver.FindElements(locator);

            foreach (IWebElement match in matches)
            {
                if (match.Displayed)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
