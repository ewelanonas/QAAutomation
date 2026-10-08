using System.Text;

namespace QAAutomation.Core.Utilities
{
    /// <summary>
    /// Turns a scenario title into something safe to use as a folder name.
    ///
    /// Scenario titles are sentences: they contain spaces, quotes, colons and slashes, none of
    /// which belong in a Windows path. Anything outside A-Z, a-z, 0-9, hyphen and underscore
    /// becomes a hyphen, and the result is capped so a long title cannot push the full path
    /// past the operating system limit.
    ///
    /// A plain foreach over the characters, no regex: there is nothing to learn before reading
    /// it, and a breakpoint can go on any line.
    /// </summary>
    public static class FileNameSanitiser
    {
        private const int MaximumLength = 80;
        private const char ReplacementCharacter = '-';

        public static string ToSafeFileName(string value)
        {
            if (value == null)
            {
                return "unnamed";
            }

            StringBuilder safeName = new StringBuilder();

            foreach (char character in value)
            {
                if (safeName.Length >= MaximumLength)
                {
                    break;
                }

                if (IsAllowed(character))
                {
                    safeName.Append(character);
                }
                else
                {
                    safeName.Append(ReplacementCharacter);
                }
            }

            string result = safeName.ToString();
            if (result.Length == 0)
            {
                return "unnamed";
            }

            return result;
        }

        private static bool IsAllowed(char character)
        {
            if (character >= 'A' && character <= 'Z')
            {
                return true;
            }

            if (character >= 'a' && character <= 'z')
            {
                return true;
            }

            if (character >= '0' && character <= '9')
            {
                return true;
            }

            if (character == '-' || character == '_')
            {
                return true;
            }

            return false;
        }
    }
}
