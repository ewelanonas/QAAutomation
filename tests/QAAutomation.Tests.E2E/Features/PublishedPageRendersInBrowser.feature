@e2e @smoke @marketing
Feature: Published page renders in the browser
  The marketing site publishes its pages through a read-only content API and serves the same
  pages to visitors. Those two views of one page have to agree. If the API lists a page the
  browser cannot render, or renders under a different name, something between the content
  store and the public site is broken - and neither an API test nor a UI test on its own can
  see it.

  # WHAT AN END-TO-END SCENARIO IS FOR, AND WHY THERE IS ONLY ONE.
  #
  # This is API-SEEDED UI VERIFICATION. The API decides which page to check and what its title
  # should contain; the browser then proves that page really renders with that title. Nothing
  # is hard-coded between the two halves, so the scenario follows the content rather than a
  # snapshot of it.
  #
  # Notice the shape, because it is the shape to copy:
  #   - the API supplies the facts (which page, what title, what address)
  #   - the browser is the thing under test
  #   - the assertion compares the two
  #
  # There is ONE scenario here on purpose. End-to-end tests are the slowest and the hardest to
  # diagnose, so this suite proves only that the layers join up. Everything about the API's
  # own behaviour is covered in the API suite, and everything about the page's own behaviour is
  # covered in the UI suite. The rule is: prefer the lowest layer that can prove it.
  #
  # READ-ONLY. The system under test is a THIRD-PARTY PRODUCTION WEBSITE. This scenario makes
  # one GET request and opens one page. Nothing here submits anything, and nothing here ever
  # may - see the contact form note in tests\QAAutomation.Tests.UI\Features\EnquiryForm.feature.

  Scenario: A page published through the content API renders with a matching title in the browser
    Given the content API supplies the page with the configured known slug
    When the visitor opens that page in the browser
    Then the browser page title mentions the title the content API reported
