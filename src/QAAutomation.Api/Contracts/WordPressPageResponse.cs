using System.Text.Json.Serialization;

namespace QAAutomation.Api.Contracts
{
    /// <summary>
    /// One page as the content API returns it.
    ///
    /// Only the four fields the suite actually asks for are modelled, because every request
    /// sends _fields=id,slug,title,link. Asking for less is deliberate: a narrow request is
    /// cheaper for a production site and a narrow contract has less to drift.
    ///
    /// The JsonPropertyName attributes are spelled out rather than relying on a naming policy,
    /// so the mapping from wire name to property is visible in one place.
    /// </summary>
    public record WordPressPageResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        [JsonPropertyName("link")]
        public string Link { get; set; }

        [JsonPropertyName("title")]
        public RenderedText Title { get; set; }
    }
}
