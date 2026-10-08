using System;
using System.Runtime.CompilerServices;
using Reqnroll.BoDi;

namespace QAAutomation.Tests.Api
{
    /// <summary>
    /// Gives the first scenario of a run enough time to build its services.
    ///
    /// THE PROBLEM THIS SOLVES, because it will look like a mystery otherwise.
    ///
    /// Scenarios in different feature files run two at a time, so both feature files start
    /// within milliseconds of each other. The first one to start runs the
    /// [ScenarioDependencies] factory while the .NET runtime is still loading and compiling
    /// everything that factory touches - configuration, the HTTP client factory, the typed API
    /// client. On a cold machine, or on a build agent that has just compiled the solution, that
    /// first call can take a little over a second.
    ///
    /// Reqnroll's container lets a second thread wait only ONE second for that to finish, then
    /// gives up with "Concurrent object resolution timeout (potential circular dependency)".
    /// There is no circular dependency here - the first call was simply slow - but the second
    /// feature fails in its set-up, and it fails only on the first run after a build. That is
    /// the worst kind of failure: it looks random, it never reproduces locally on a second
    /// attempt, and it wastes an afternoon.
    ///
    /// So the wait is raised to a minute. Nothing waits that long in practice; the point is
    /// that a slow first start is allowed to finish instead of being called a deadlock.
    ///
    /// WHY THIS IS NOT A HOOK. A [BeforeTestRun] hook would be the obvious home for it, but a
    /// hook class is itself built by the container, so it runs too late to change how the
    /// container waits. [ModuleInitializer] runs once when the test assembly is loaded, before
    /// any scenario, which is the only point early enough.
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
