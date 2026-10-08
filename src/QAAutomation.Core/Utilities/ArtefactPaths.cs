using System;
using System.Globalization;
using System.IO;

namespace QAAutomation.Core.Utilities
{
    /// <summary>
    /// Works out where this scenario's failure artefacts go, and creates the folder.
    ///
    /// The shape is:
    ///   &lt;repository root&gt;\test-results\&lt;suite&gt;\&lt;scenario title&gt;-&lt;yyyyMMdd-HHmmss&gt;\
    ///
    /// Why the walk-up: a test assembly runs from bin\Debug\net10.0, so a relative path would
    /// drop the evidence inside the build output where nobody thinks to look. Walking up to the
    /// folder holding QAAutomation.sln puts it somewhere a QA can find in two clicks. Set
    /// QAAUTOMATION_Artefacts__Directory to an absolute path to override this entirely, which is
    /// what a build agent with an artefact-staging folder would do.
    /// </summary>
    public static class ArtefactPaths
    {
        private const string SolutionFileName = "QAAutomation.sln";
        private const string TimestampFormat = "yyyyMMdd-HHmmss";

        public static string ForScenario(string configuredDirectory, string suiteName, string scenarioTitle)
        {
            string artefactRoot = configuredDirectory;
            if (Path.IsPathRooted(configuredDirectory) == false)
            {
                string repositoryRoot = FindRepositoryRoot();
                artefactRoot = Path.Combine(repositoryRoot, configuredDirectory);
            }

            string timestamp = DateTime.Now.ToString(TimestampFormat, CultureInfo.InvariantCulture);
            string folderName = FileNameSanitiser.ToSafeFileName(scenarioTitle) + "-" + timestamp;
            string safeSuiteName = FileNameSanitiser.ToSafeFileName(suiteName);

            string scenarioFolder = Path.Combine(artefactRoot, safeSuiteName, folderName);
            Directory.CreateDirectory(scenarioFolder);

            return scenarioFolder;
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo candidate = new DirectoryInfo(AppContext.BaseDirectory);

            while (candidate != null)
            {
                string solutionPath = Path.Combine(candidate.FullName, SolutionFileName);
                if (File.Exists(solutionPath))
                {
                    return candidate.FullName;
                }

                candidate = candidate.Parent;
            }

            // No solution file above us. Rather than fail a teardown, write next to the assembly.
            return AppContext.BaseDirectory;
        }
    }
}
