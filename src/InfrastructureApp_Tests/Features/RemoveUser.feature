Feature: Remove User
  As an authorized Administrator
  I want to permanently remove a user account from the system
  So that I can ensure that inactive users no longer have access to sensitive data

  Background:
    Given a user with username "adminuser" and password "AdminPassword123!" exists
    And the user "adminuser" has the "Admin" role
    And a user with username "targetuser" and password "UserPassword123!" exists
    And the user "targetuser" has the "User" role

  Scenario: Only admins can see the Remove User option
    When I log in with username "adminuser" and password "AdminPassword123!" for RemoveUser
    And I am authenticated
    And I navigate to the Admin page
    Then I should see a "Remove" button for user "targetuser"

  Scenario: Non-admins cannot see the Remove User option
    When I log in with username "targetuser" and password "UserPassword123!" for RemoveUser
    And I am authenticated
    And I navigate to the Admin page
    Then I should see an error message "Access Denied" or be redirected

  Scenario: Admin can successfully remove a user after confirmation
    When I log in with username "adminuser" and password "AdminPassword123!" for RemoveUser
    And I am authenticated
    And I navigate to the Admin page
    And I click "Remove" for user "targetuser"
    Then I should see a confirmation modal for "targetuser"
    When I confirm the deletion
    Then I should not see "targetuser" in the user list

  Scenario: Admin cannot remove their own account
    When I log in with username "adminuser" and password "AdminPassword123!" for RemoveUser
    And I am authenticated
    And I navigate to the Admin page
    Then I should not see a "Remove" button for user "adminuser"

  Scenario: Removed user session is invalidated
    When I log in with username "targetuser" and password "UserPassword123!" for RemoveUser
    And I am authenticated
    And "adminuser" removes "targetuser"
    And I navigate to the Dashboard page
    Then I should be redirected to the Login page
