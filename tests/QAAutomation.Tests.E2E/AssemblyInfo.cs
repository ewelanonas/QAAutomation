// THE END-TO-END SUITE RUNS ONE SCENARIO AT A TIME, ON PURPOSE.
//
// There is deliberately NO [assembly: Parallelizable(...)] attribute in this file. Without
// one, NUnit runs everything here serially, and that is the behaviour we want.
//
// Why serial, when the UI suite runs two at a time:
//
// 1. An end-to-end scenario is the most expensive thing in the repository. It makes a real
//    HTTP call AND starts a real browser, so it costs an API request plus a Chrome process.
//    Running several at once multiplies both against a THIRD-PARTY PRODUCTION WEBSITE, and a
//    test suite has no business putting load on someone else's live site.
//
// 2. An end-to-end failure is the hardest kind to read. Running serially means the test
//    output, the browser console log and the captured artefacts all belong to one scenario,
//    in order, with nothing interleaved.
//
// 3. This suite is small by design. End-to-end coverage is the top of the pyramid - a handful
//    of journeys that prove the layers join up. There is nothing here for parallelism to
//    speed up.
//
// If this suite ever grows enough to need parallelism, copy the attributes from
// tests\QAAutomation.Tests.UI\AssemblyInfo.cs and keep the worker count low for the same
// reasons given there.
