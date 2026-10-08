using System.Collections.Generic;
using System.Net.Http.Headers;

namespace QAAutomation.Core.Utilities
{
    /// <summary>
    /// Reads a single response header value.
    ///
    /// An HTTP header can legally appear more than once, so the framework hands back a
    /// collection. Nearly every header we care about has one value, so this helper takes the
    /// first and returns null when the header is absent. That keeps the "header missing" case
    /// a plain null check in the step rather than an exception.
    ///
    /// Never pass an Authorization header or a cookie through here into a log or an assertion
    /// message.
    /// </summary>
    public static class HeaderValueReader
    {
        public static string ReadFirstValue(HttpResponseHeaders headers, string headerName)
        {
            if (headers == null)
            {
                return null;
            }

            IEnumerable<string> values;
            bool headerExists = headers.TryGetValues(headerName, out values);
            if (headerExists == false)
            {
                return null;
            }

            foreach (string value in values)
            {
                return value;
            }

            return null;
        }
    }
}
