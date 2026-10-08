using NUnit.Framework;

// Scenarios in different feature files may run at the same time, two at a time.
//
// Two, and not more, on purpose: the system under test is a THIRD-PARTY PRODUCTION WEBSITE.
// These are read-only GET requests, but a test suite has no business putting load on someone
// else's live site. Raise this only against an environment we own.
[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelism(2)]
