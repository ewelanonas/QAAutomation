@api @regression @marketing
Feature: Page lookup
  A single page can be fetched either by the slug that appears in its public address
  or by its identifier. Anything linking to a page relies on that: a slug has to
  resolve to exactly one page, and an address that does not exist has to be refused
  rather than answered with something approximate.

  # The slug used below is live third-party content. The same value is configured as
  # Site:KnownPageSlug in appsettings.json, which is how the end-to-end suite reaches
  # the matching web page, so if the company ever renames that page both the setting
  # and this feature file change together.
  #
  # The identifier for the missing-page scenario is NOT written here. It comes from
  # Site:MissingPageId, because "an identifier that does not exist" is an environment
  # fact rather than part of the behaviour being specified.

  @smoke
  Scenario: Looking up a published page by its slug returns one fully formed page
    When the page with the slug "technical" is requested
    Then the page lookup succeeds
    And exactly one page is returned
    And the returned page carries an identifier, a slug, a title and a link

  Scenario: Looking up a page identifier that does not exist is refused
    When the page with an identifier that does not exist is requested
    Then the page lookup reports that the page was not found
