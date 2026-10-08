using System;

namespace QAAutomation.Core.Configuration
{
    /// <summary>
    /// Thrown when a configuration value is missing or unusable.
    ///
    /// The message names the exact configuration key and, where it is safe, the offending
    /// value. It deliberately never dumps the whole configuration object: a value can be an
    /// injected key or a token, and an exception message ends up in logs and build output.
    /// </summary>
    public class TestConfigurationException : Exception
    {
        /// <summary>
        /// Key endings that mark a value as a secret. The value behind one of these is never
        /// printed, whatever is wrong with it.
        ///
        /// Today no real secret can reach a message here, because the only checks that run on
        /// Api:ApiKey fire when the value is missing or blank. That is luck, not a safeguard:
        /// the day someone adds a length or format check on the key, the message would carry
        /// the key itself into the build output. This closes that door up front.
        /// </summary>
        private static readonly string[] SecretKeyEndings = new string[]
        {
            "Key",
            "Secret",
            "Token",
            "Password"
        };

        /// <summary>Stands in for a secret value, so the message still reads as a sentence.</summary>
        private const string HiddenValueText = "(hidden, because this key holds a secret)";

        public TestConfigurationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Builds a message of the shape: Configuration key 'Site:BaseUrl' is invalid: must be
        /// an absolute https URL. Value was: 'ftp://example.com'.
        ///
        /// For a key named as a secret the value is replaced, so the message still names the
        /// key and the problem - which is what a reader needs - without printing the value.
        /// </summary>
        public static TestConfigurationException ForKey(string configurationKey, string problem, string offendingValue)
        {
            string valueText = "(not set)";
            if (offendingValue != null)
            {
                valueText = "'" + offendingValue + "'";
            }

            if (HoldsASecret(configurationKey))
            {
                valueText = HiddenValueText;
            }

            string message = "Configuration key '" + configurationKey + "' is invalid: " + problem
                + ". Value was: " + valueText + ".";

            return new TestConfigurationException(message);
        }

        /// <summary>
        /// True when the key name ends with one of the secret endings, compared without regard
        /// to case so 'Api:apikey' is treated the same as 'Api:ApiKey'.
        /// </summary>
        private static bool HoldsASecret(string configurationKey)
        {
            if (configurationKey == null)
            {
                return false;
            }

            foreach (string secretKeyEnding in SecretKeyEndings)
            {
                if (configurationKey.EndsWith(secretKeyEnding, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
