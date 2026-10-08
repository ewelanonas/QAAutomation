@ui @regression @marketing
Feature: Primary navigation
  The header navigation is how a visitor moves around the site, so following a link in it
  has to take the visitor to the page the link names.

  # The link name below is live website content, held in one place in the feature file so a
  # copy change on the site is a one-line edit here. Note that the site labels its contact
  # link "Contact Sales", not "Contact Us" - a reminder that link text is content, not
  # contract.
  #
  # The check is on the page title rather than on a heading, because the pages this
  # navigation leads to are marketing pages whose headings change with the marketing.

  Scenario: The About Us navigation link opens the About Us page
    Given the visitor opens the Landmark home page
    When the visitor follows the "About Us" link in the primary navigation
    Then the browser page title mentions "About Us"
