@ui @regression @marketing
Feature: Home page
  A visitor arriving at the site should be able to tell what the company sells and should
  be able to get anywhere else on the site. Those two things are what the home page is for,
  so those two things are what is checked here.

  # WHAT THIS FEATURE DELIBERATELY NEVER ASSERTS ON, AND WHY.
  #
  # The home page leads with a rotating hero carousel and a row of statistic counters that
  # animate upwards when they scroll into view. Neither is ever asserted on anywhere in this
  # suite. Whatever a carousel is showing depends on how long the page has been open, and
  # whatever a counter reads depends on how far its animation has run, so an assertion on
  # either would pass or fail on timing rather than on behaviour. That is a flaky test by
  # construction, and a flaky test teaches the team to re-run the build instead of reading it.
  #
  # The carousel is also why nothing here asserts on a heading: the page carries FOUR h1
  # elements, one per slide, so "the heading" is not a single thing on this page.
  #
  # The page title is server-rendered and stable, so that is what identifies the page.

  @smoke
  Scenario: The home page identifies the product in its page title
    Given the visitor opens the Landmark home page
    Then the browser page title is the expected home page title

  Scenario: The home page shows the primary navigation
    Given the visitor opens the Landmark home page
    Then the primary navigation is visible
