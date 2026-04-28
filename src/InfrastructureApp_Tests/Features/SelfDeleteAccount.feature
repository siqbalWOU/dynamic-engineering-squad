Feature: Self Delete Account
  As a registered user
  I want to permanently delete my account and all associated personal data from within my settings
  So that I can exercise my right to be forgotten

  Background:
    Given a user with username "deleteuser" and password "Password123!" exists
    And the user's email is confirmed

  Scenario: User can see the Delete Account option on the Dashboard
    When I log in with username "deleteuser" and password "Password123!" for SelfDelete
    And I am authenticated
    And I navigate to the Dashboard page
    Then I should see a "Delete Account" option

  Scenario: User is prompted for password and sees warning before deletion
    When I log in with username "deleteuser" and password "Password123!" for SelfDelete
    And I am authenticated
    And I navigate to the Dashboard page
    And I click "Delete Account"
    Then I should be on the "Delete Account" confirmation page
    And I should see a warning that this action is irreversible
    And I should see a field to enter my current password

  Scenario: User successfully deletes account with correct password
    When I log in with username "deleteuser" and password "Password123!" for SelfDelete
    And I am authenticated
    And I navigate to the Dashboard page
    And I click "Delete Account"
    And I enter "Password123!" as my current password
    And I confirm the account deletion
    Then I should be redirected to the home page
    And I should not be authenticated
    And the user "deleteuser" should no longer exist in the system

  Scenario: User fails to delete account with incorrect password
    When I log in with username "deleteuser" and password "Password123!" for SelfDelete
    And I am authenticated
    And I navigate to the Dashboard page
    And I click "Delete Account"
    And I enter "WrongPassword" as my current password
    And I confirm the account deletion
    Then I should see an error message "Incorrect password."
    And I should still be authenticated
