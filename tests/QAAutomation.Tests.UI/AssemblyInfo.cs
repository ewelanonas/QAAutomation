using NUnit.Framework;

// Scenarios in different feature files may run at the same time, two at a time.
//
// Two, and not more, for two reasons. Every scenario starts its own browser, so the number of
// workers is the number of Chrome processes running at once - that is memory, and on a build
// agent it is the thing that falls over first. And the system under test is a THIRD-PARTY
// PRODUCTION WEBSITE: a test suite has no business putting load on someone else's live site.
//
// One browser per worker, never shared: IWebDriver is not thread-safe, and the driver is
// registered scoped so each scenario gets its own.
[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelism(2)]
