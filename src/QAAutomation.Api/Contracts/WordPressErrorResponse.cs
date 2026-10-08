using System.Text.Json.Serialization;

namespace QAAutomation.Api.Contracts
{
    /// <summary>
    /// The error body the content API returns with a refusal, for example
    /// { "code": "rest_invalid_param", "message": "Invalid parameter(s): per_page" }.
    ///
    /// Code is the part worth asserting on: it is the machine-readable contract and it does not
    /// change when someone rewords the message. Message is carried for the failure output only.
    /// </summary>
    public record WordPressErrorResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
