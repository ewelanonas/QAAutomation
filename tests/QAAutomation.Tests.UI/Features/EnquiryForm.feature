@ui @regression @marketing
Feature: Enquiry form
  A visitor who wants to be contacted uses the enquiry form. Before they type anything, the
  form has to tell them which details are required and which are optional, and it has to
  hold what they type.

  # ===================================================================================
  # READ THIS BEFORE ADDING A SCENARIO TO THIS FILE.
  #
  # THIS IS A PRODUCTION WEBSITE AND THIS FEATURE IS READ-ONLY. THE FORM IS NEVER SUBMITTED.
  #
  # The site belongs to Landmark Systems and is their live, customer-facing site. This form
  # is their real sales enquiry form, so a successful submit does not land in a test
  # database - it creates a REAL SALES ENQUIRY that a real person has to read and answer.
  #
  # There is also nothing to gain from submitting it. The live form carries the HTML
  # "novalidate" attribute, which switches browser validation off completely, so there is NO
  # client-side validation to assert on. The only validation that exists runs on the server,
  # behind the submit we are not allowed to make.
  #
  # So ContactUsPage deliberately has NO submit method, and none may be added. The absence
  # is the control: if the method does not exist, nobody can "just finish" this scenario
  # later by calling it.
  #
  # The scenarios below prove the same mandatory-field rule a submit would have exercised,
  # by reading what the form itself declares, and they prove the form holds what is typed.
  # Both are reads. Nothing is written to Landmark's systems.
  # ===================================================================================

  # The field names are the labels a visitor reads on the page. The four rows are two
  # representatives of each partition - fields the form declares as required, and fields it
  # declares as optional - rather than all eight fields, because a fifth required field
  # would add runtime and no coverage. The "mandatory" column is the expected outcome.
  Scenario Outline: The enquiry form declares which of its fields are mandatory
    Given the visitor opens the Landmark contact page
    Then the "<fieldName>" field is mandatory: <mandatory>

    Examples: mandatory and optional fields (equivalence partitions)
      | fieldName     | mandatory |
      | First Name    | true      |
      | Email address | true      |
      | Company Name  | false     |
      | Your Enquiry  | false     |

  # The wording typed in below says plainly that it is an automated test, so that if it ever
  # did reach a human by some other route it would be obvious what it was.
  Scenario: The enquiry form holds the details the visitor types without submitting them
    Given the visitor opens the Landmark contact page
    When the visitor types "Automated test enquiry - please ignore" into the "Your Enquiry" field
    Then the "Your Enquiry" field holds "Automated test enquiry - please ignore"
