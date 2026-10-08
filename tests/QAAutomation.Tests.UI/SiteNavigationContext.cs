namespace QAAutomation.Tests.UI
{
    /// <summary>
    /// What a navigation scenario remembers between its steps.
    ///
    /// A When step puts what it saw in here and a Then step reads it. Typed properties, not a
    /// ScenarioContext string bag: a typo in a property name fails the build, a typo in a
    /// dictionary key fails at run time with a confusing message.
    ///
    /// Registered scoped, so every scenario starts with an empty one.
    ///
    /// It lives at the test project root rather than in Core because it is this suite's own
    /// state, and Core must not know anything about a particular suite.
    /// </summary>
    public class SiteNavigationContext
    {
        /// <summary>The browser tab title last read from the page.</summary>
        public string PageTitle { get; set; }

        /// <summary>The address the visitor ended up on after following a link.</summary>
        public string LandedUrl { get; set; }

        /// <summary>Whether the header navigation was visible when it was last looked at.</summary>
        public bool NavigationVisible { get; set; }
    }
}
