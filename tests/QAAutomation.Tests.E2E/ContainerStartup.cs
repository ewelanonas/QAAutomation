using System;
using System.Runtime.CompilerServices;
using Reqnroll.BoDi;

namespace QAAutomation.Tests.E2E
{
    /// <summary>
    /// Gives the first scenario of a run enough time to build its services.
    ///
    /// THE PROBLEM THIS SOLVES, because it will look like a mystery otherwise.
    ///
    /// The first scenario of a run calls the [ScenarioDependencies] factory while the .NET
    /// runtime is still loading and compiling everything that factory touches. This suite's
    /// factory is the heaviest of the three, because it builds the configuration, the HTTP
    /// stack, the generated API client, the browser driver stack and the page objects. On a
    /// cold machine, or on a build agent that has just compiled the solution, that first call
    /// can take a little over a second.
    ///
    /// Reqnroll's container lets a second thread wait only ONE second for that to finish, then
    /// gives up with "Concurrent object resolution timeout (potential circular dependency)".
    /// There is no circular dependency - the first call was simply slow - but the scenario
    /// fails in its set-up, and it fails only on the first run after a build. That is the worst
    /// kind of failure: it looks random, it never reproduces on a second attempt, and it wastes
    /// an afternoon.
    ///
    /// This suite runs serially, so it is far less exposed than the UI suite. The guard is kept
    /// anyway: it costs nothing, and the day somebody adds a second end-to-end feature file and
    /// turns parallelism on, the trap is already disarmed.
    ///
    /// WHY THIS IS NOT A HOOK. A [BeforeTestRun] hook would be the obvious home for it, but a
    /// hook class is itself built by the container, so it runs too late to change how the
    /// container waits. [ModuleInitializer] runs once when the test assembly is loaded, before
    /// any scenario, which is the only point early enough.
    ///
    /// This file is duplicated in the API and UI test projects rather than shared, because
    /// project-structure.md forbids one test project referencing another, and code-style.md
    /// prefers plain duplication over a premature abstraction.
    /// </summary>
    internal static class ContainerStartup
    {
        private static readonly TimeSpan FirstResolutionTimeout = TimeSpan.FromSeconds(60);

        [ModuleInitializer]
        internal static void AllowTimeForTheFirstResolution()
        {
            ObjectContainer.DefaultConcurrentObjectResolutionTimeout = FirstResolutionTimeout;
        }
    }
}
