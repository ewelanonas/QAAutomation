using System;
using System.Collections.ObjectModel;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace QAAutomation.Core.Utilities
{
    /// <summary>
    /// The one and only wait helper in this repository.
    ///
    /// Selenium does not wait for anything by itself: it acts immediately and throws if the
    /// element is not ready yet. So every wait in the suite goes through this class. There is
    /// no Thread.Sleep anywhere, and no hand-rolled polling loop anywhere.
    ///
    /// Every lambda in the repository lives in this file. WebDriverWait.Until accepts nothing
    /// but a lambda, which is exception 2 in .kiro/steering/code-style.md. Writing them once,
    /// here, is what keeps that exception from spreading across the suite.
    /// </summary>
    public class ElementWaits
    {
        private readonly IWebDriver driver;
        private readonly TimeSpan timeout;

        public ElementWaits(IWebDriver driver, TimeSpan timeout)
        {
            this.driver = driver;
            this.timeout = timeout;
        }

        /// <summary>Waits until one matching element is on the page and visible, then returns it.</summary>
        public IWebElement WaitUntilVisible(By locator)
        {
            WebDriverWait wait = CreateWait();

            // code-style.md exception 2: WebDriverWait.Until takes a lambda and nothing else.
            IWebElement element = wait.Until(currentDriver => FindVisible(currentDriver, locator));
            return element;
        }

        /// <summary>
        /// Waits until one matching element is visible AND enabled, then returns it. Use this
        /// before a click: a visible but disabled button still throws when clicked.
        /// </summary>
        public IWebElement WaitUntilClickable(By locator)
        {
            WebDriverWait wait = CreateWait();

            // code-style.md exception 2: WebDriverWait.Until takes a lambda and nothing else.
            IWebElement element = wait.Until(currentDriver => FindClickable(currentDriver, locator));
            return element;
        }

        /// <summary>
        /// Waits until at least one match is visible and returns the FIRST visible one.
        ///
        /// This is the method to reach for whenever a page renders the same block twice, such as
        /// a desktop and a mobile copy of the same navigation bar. A plain FindElement would
        /// return the hidden copy and the click would fail with ElementNotInteractableException.
        /// </summary>
        public IWebElement WaitUntilFirstVisible(By locator)
        {
            WebDriverWait wait = CreateWait();

            // code-style.md exception 2: WebDriverWait.Until takes a lambda and nothing else.
            IWebElement element = wait.Until(currentDriver => FindFirstVisible(currentDriver, locator));
            return element;
        }

        /// <summary>Waits until nothing matching the locator is visible on the page any more.</summary>
        public void WaitUntilGone(By locator)
        {
            WebDriverWait wait = CreateWait();

            // code-style.md exception 2: WebDriverWait.Until takes a lambda and nothing else.
            wait.Until(currentDriver => IsGone(currentDriver, locator));
        }

        /// <summary>Waits until the browser title contains the given text, case-insensitively.</summary>
        public void WaitUntilTitleContains(string expectedTitlePart)
        {
            WebDriverWait wait = CreateWait();

            // code-style.md exception 2: WebDriverWait.Until takes a lambda and nothing else.
            wait.Until(currentDriver => TitleContains(currentDriver, expectedTitlePart));
        }

        private WebDriverWait CreateWait()
        {
            WebDriverWait wait = new WebDriverWait(this.driver, this.timeout);

            // A single-page app can re-render between finding an element and using it. Ignoring
            // these two means the wait retries inside its own bounded timeout instead of failing
            // on the first blink. It does NOT make the wait longer.
            wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));

            return wait;
        }

        // The helpers below do the real work. They are separate from the lambdas on purpose:
        // the lambda stays one line, and these read as plain methods anyone can breakpoint.

        private static IWebElement FindVisible(IWebDriver currentDriver, By locator)
        {
            IWebElement element = currentDriver.FindElement(locator);
            if (element.Displayed == false)
            {
                // Returning null tells WebDriverWait to try again.
                return null;
            }

            return element;
        }

        private static IWebElement FindClickable(IWebDriver currentDriver, By locator)
        {
            IWebElement element = currentDriver.FindElement(locator);
            if (element.Displayed == false)
            {
                return null;
            }

            if (element.Enabled == false)
            {
                return null;
            }

            return element;
        }

        private static IWebElement FindFirstVisible(IWebDriver currentDriver, By locator)
        {
            ReadOnlyCollection<IWebElement> matches = currentDriver.FindElements(locator);

            foreach (IWebElement match in matches)
            {
                if (match.Displayed)
                {
                    return match;
                }
            }

            return null;
        }

        private static bool IsGone(IWebDriver currentDriver, By locator)
        {
            ReadOnlyCollection<IWebElement> matches = currentDriver.FindElements(locator);

            foreach (IWebElement match in matches)
            {
                if (match.Displayed)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TitleContains(IWebDriver currentDriver, string expectedTitlePart)
        {
            string actualTitle = currentDriver.Title;
            if (actualTitle == null)
            {
                return false;
            }

            return actualTitle.IndexOf(expectedTitlePart, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
