Feature: Search Users
  As an authorized Administrator
  I want to be able to search the user management list by username
  So that I can quickly locate a specific account without manually paging through the entire user base

  Background:
    Given a user with username "adminuser" and password "AdminPassword123!" exists
    And the user "adminuser" has the "Admin" role
    And a user with username "alice" exists
    And a user with username "bob" exists
    And a user with username "charlie" exists

  Scenario: Admin can see the search input field
    When I log in with username "adminuser" and password "AdminPassword123!"
    And I navigate to the Admin page
    Then I should see a search input field

  Scenario: Admin searches for a user by partial username
    When I log in with username "adminuser" and password "AdminPassword123!"
    And I navigate to the Admin page
    And I enter "ali" into the search field
    Then I should see "alice" in the user list
    And I should not see "bob" in the user list
    And I should not see "charlie" in the user list

  Scenario: Admin search is case-insensitive
    When I log in with username "adminuser" and password "AdminPassword123!"
    And I navigate to the Admin page
    And I enter "ALICE" into the search field
    Then I should see "alice" in the user list

  Scenario: Admin searches for a non-existent user
    When I log in with username "adminuser" and password "AdminPassword123!"
    And I navigate to the Admin page
    And I enter "nonexistent" into the search field
    Then I should see an empty state message "No users found matching 'nonexistent'"

  Scenario: Admin clears the search field
    When I log in with username "adminuser" and password "AdminPassword123!"
    And I navigate to the Admin page
    And I enter "alice" into the search field
    And I clear the search field
    Then I should see "alice" in the user list
    And I should see "bob" in the user list
    And I should see "charlie" in the user list
