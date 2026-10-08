@api @regression @marketing
Feature: Page listing
  The marketing site publishes its pages through a read-only content API.
  A visitor-facing tool, a sitemap generator or a search integration reads that list,
  so the list has to come back in a predictable shape and it has to honour the page
  size it was asked for.

  # COVERAGE NOTE - read this before adding a scenario.
  #
  # The system under test is a LIVE production website. Its content changes whenever
  # someone at the company publishes or unpublishes a page, and nobody tells us when
  # that happens.
  #
  # So NO scenario in this file asserts an exact number of pages. The rules asserted
  # here are the ones the API promises and content cannot break:
  #   - the shape of a returned page
  #   - never returning more pages than were asked for
  #   - reporting a total that is a positive whole number
  #   - which page sizes are accepted and which are refused
  #
  # A scenario that asserted "18 pages exist" would pass today and fail next week for
  # no product reason. That is the single most important lesson in this template.

  @smoke
  Scenario: Requesting the page list returns published pages
    When a page list is requested with a page size of 2
    Then the page list request succeeds
    And every returned page carries an identifier, a slug, a title and a link

  Scenario: The page list never returns more pages than the requested page size
    When a page list is requested with a page size of 2
    Then the page list request succeeds
    And no more pages are returned than were requested

  Scenario: The page list reports how many pages exist in total
    When a page list is requested with a page size of 2
    Then the page list request succeeds
    And the pagination headers report positive whole numbers

  # The accepted and rejected partitions are separate tables because their Then
  # differs, so the expected outcome lives in the Then rather than in a column
  # that would be constant for every row.
  Scenario Outline: A page size inside the documented range is accepted
    When a page list is requested with a page size of <pageSize>
    Then the page list request succeeds

    Examples: accepted page size boundaries (boundary value analysis)
      | pageSize |
      | 1        |
      | 100      |

  Scenario Outline: A page size outside the documented range is rejected
    When a page list is requested with a page size of <pageSize>
    Then the page list request is rejected as an invalid parameter

    Examples: rejected page size boundaries (boundary value analysis)
      | pageSize |
      | 0        |
      | 101      |
