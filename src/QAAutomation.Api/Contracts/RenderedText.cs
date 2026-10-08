using System.Text.Json.Serialization;

namespace QAAutomation.Api.Contracts
{
    /// <summary>
    /// WordPress does not return plain text for a title. It returns a small object with one
    /// field, like this: { "rendered": "Technical" }. The "rendered" name means "after the
    /// content filters have run", which is why a shortcode or an HTML entity can appear in it.
    ///
    /// Modelling it as its own type keeps the mapping honest. A step that wants the text asks
    /// for page.Title.Rendered, which reads the same way the JSON does.
    /// </summary>
    public record RenderedText
    {
        [JsonPropertyName("rendered")]
        public string Rendered { get; set; }
    }
}
