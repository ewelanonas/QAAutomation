using QAAutomation.Api.Contracts;

namespace QAAutomation.Tests.Api
{
    /// <summary>
    /// What one scenario remembers between its steps.
    ///
    /// A When step puts the response here and the Then steps read it. Typed properties, not a
    /// ScenarioContext string bag: a typo in a property name fails the build, a typo in a
    /// dictionary key fails at run time with a confusing message.
    ///
    /// Registered scoped, so every scenario starts with an empty one.
    ///
    /// It lives at the test project root rather than in Core because it holds API contract
    /// records. Putting it in Core would force Core to reference the Api project and reverse
    /// the one-way dependency direction in project-structure.md.
    /// </summary>
    public class PageApiContext
    {
        /// <summary>The page size the scenario asked for, kept so a Then step can compare against it.</summary>
        public int RequestedPageSize { get; set; }

        /// <summary>The answer to the most recent page-list request.</summary>
        public PageListResponse ListResponse { get; set; }

        /// <summary>The answer to the most recent single-page lookup.</summary>
        public PageLookupResponse LookupResponse { get; set; }
    }
}
