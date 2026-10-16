@api @regression @database @ignore
Feature: VAT return persistence
  A VAT return that has been stored must be readable back for the tenant that owns it, with the
  status it was saved with. This feature is a template that shows WHERE database validation
  lives: the step asks the repository for the row and asserts on what came back. It is not part
  of the default suite.

  # Ignored on purpose: there is no live database in the default run, because the system under
  # test is a public marketing website with no database to connect to. This scenario exists only
  # to demonstrate where database validation belongs. It runs once a real database and the
  # QAAUTOMATION_Database__ConnectionString secret are supplied and the @ignore tag is removed.
  Scenario: A stored VAT return can be read back for its tenant
    When the VAT return "VR-1001" for tenant "tenant-a" is read from the database
    Then the stored VAT return has status "Submitted"
