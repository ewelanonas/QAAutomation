using System.Globalization;

namespace QAAutomation.Core.Utilities
{
    /// <summary>
    /// Parses a whole number that must be greater than zero.
    ///
    /// Used on pagination headers. A count header is text on the wire, and the suite asserts
    /// that it parses to a positive whole number rather than asserting a specific count, because
    /// the content of a live site changes without notice.
    ///
    /// InvariantCulture on purpose: a build agent in another locale must read "18" the same way.
    /// </summary>
    public static class PositiveIntegerParser
    {
        public static bool TryParsePositive(string value, out int result)
        {
            result = 0;

            if (value == null)
            {
                return false;
            }

            int parsedValue;
            bool parsed = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedValue);
            if (parsed == false)
            {
                return false;
            }

            if (parsedValue <= 0)
            {
                return false;
            }

            result = parsedValue;
            return true;
        }
    }
}
