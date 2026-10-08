namespace QAAutomation.Tests.E2E
{
    /// <summary>
    /// What an end-to-end scenario remembers as it crosses from the API to the browser.
    ///
    /// This is the hand-off. The Given asks the content API which page to look at and what its
    /// title should say, and writes the answers in here. The When reads the address out of here
    /// and opens it. The Then compares what the browser showed against what the API reported.
    ///
    /// Typed properties, not a ScenarioContext string bag: a typo in a property name fails the
    /// build, a typo in a dictionary key fails at run time with a confusing message.
    ///
    /// Registered scoped, so every scenario starts with an empty one.
    ///
    /// It lives at the test project root rather than in Core because it holds an API-shaped
    /// value. Putting it in Core would force Core to reference the Api project, and the
    /// dependency direction in project-structure.md only runs one way: tests -> src -> Core.
    /// </summary>
    public class CrossLayerContext
    {
        /// <summary>The slug the content API reported for the page under test.</summary>
        public string ApiSlug { get; set; }

        /// <summary>The title the content API reported for that page.</summary>
        public string ApiTitle { get; set; }

        /// <summary>The public address the content API reported for that page.</summary>
        public string ApiLink { get; set; }

        /// <summary>The browser tab title read after opening that address.</summary>
        public string BrowserTitle { get; set; }
    }
}
