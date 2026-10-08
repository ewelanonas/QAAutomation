using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QAAutomation.Api.Contracts;
using Refit;

namespace QAAutomation.Api.Clients
{
    /// <summary>
    /// The typed contract for the content API of the system under test.
    ///
    /// Two things to notice, because both are house rules:
    ///
    /// 1. Every method returns IApiResponse&lt;T&gt;, never a bare T. A bare return type makes
    ///    Refit throw on a 400 or a 404, and this suite needs to inspect those - a refusal is
    ///    frequently the thing under test. With IApiResponse the status code, the headers and
    ///    the error body all come back as data.
    /// 2. The query-string names are spelled with AliasAs. The API uses snake_case
    ///    (per_page, _fields), the C# parameters use the usual casing, and AliasAs is the
    ///    bridge. No string concatenation, no manually built query strings.
    ///
    /// The base address is NOT here. It comes from configuration through ApiRegistration, so
    /// pointing the suite at another environment is a settings change, not a code change.
    /// </summary>
    public interface IWordPressApi
    {
        /// <summary>Lists pages, newest first, limited to the requested page size.</summary>
        [Get("/pages")]
        Task<IApiResponse<List<WordPressPageResponse>>> GetPagesAsync(
            [AliasAs("per_page")] int perPage,
            [AliasAs("_fields")] string fields,
            CancellationToken cancellationToken);

        /// <summary>
        /// Searches pages by slug. The API answers with a list even when at most one page can
        /// match, which is why this returns a list and the caller reports how many matched.
        /// </summary>
        [Get("/pages")]
        Task<IApiResponse<List<WordPressPageResponse>>> GetPagesBySlugAsync(
            [AliasAs("slug")] string slug,
            [AliasAs("_fields")] string fields,
            CancellationToken cancellationToken);

        /// <summary>Reads one page by its identifier. Answers 404 when no such page exists.</summary>
        [Get("/pages/{pageId}")]
        Task<IApiResponse<WordPressPageResponse>> GetPageAsync(
            int pageId,
            [AliasAs("_fields")] string fields,
            CancellationToken cancellationToken);
    }
}
