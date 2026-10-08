using OpenQA.Selenium;

namespace QAAutomation.Core.Utilities
{
    /// <summary>
    /// Builds a locator from a data-testid attribute.
    ///
    /// DELIBERATELY UNUSED in this template, and that is worth understanding rather than
    /// deleting. .kiro/steering/ui-automation-selenium.md makes data-testid the first-choice
    /// locator strategy, because a test id is put there for testing and will not move when a
    /// designer changes a class name.
    ///
    /// The template's system under test is a third-party WordPress site. It has no test ids,
    /// and we cannot add any, so the page objects fall back to the most stable selectors that
    /// actually exist there. That is a documented exception, explained at the top of every page
    /// object and in the README.
    ///
    /// This class ships anyway because on a product we own, data-testid becomes the primary
    /// strategy again on day one, and this is the helper to use.
    /// </summary>
    public static class TestIdLocator
    {
        private const string TestIdAttributeName = "data-testid";

        public static By ByTestId(string testId)
        {
            string cssSelector = "[" + TestIdAttributeName + "='" + testId + "']";
            return By.CssSelector(cssSelector);
        }
    }
}
