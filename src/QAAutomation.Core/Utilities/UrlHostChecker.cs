using System;

namespace QAAutomation.Core.Utilities
{
    /// <summary>
    /// Checks that two URLs point at the same host.
    ///
    /// This guards the end-to-end hand-off. An end-to-end scenario reads a link out of an API
    /// response and then opens it in the browser. That link is data returned by a server, so it
    /// is untrusted input: if it ever pointed somewhere else, the suite would drive a real
    /// browser to a site nobody intended to visit. The step checks the host first and fails with
    /// a clear message instead of navigating.
    /// </summary>
    public static class UrlHostChecker
    {
        public static bool HasSameHost(string expectedBaseUrl, string candidateUrl)
        {
            Uri expectedUri;
            bool expectedParsed = Uri.TryCreate(expectedBaseUrl, UriKind.Absolute, out expectedUri);
            if (expectedParsed == false)
            {
                return false;
            }

            Uri candidateUri;
            bool candidateParsed = Uri.TryCreate(candidateUrl, UriKind.Absolute, out candidateUri);
            if (candidateParsed == false)
            {
                return false;
            }

            return string.Equals(expectedUri.Host, candidateUri.Host, StringComparison.OrdinalIgnoreCase);
        }
    }
}
