Feature: Ban User
  As an authorized Administrator
  I want to be able to ban malicious or rule-breaking users
  So that I can maintain a safe, high-quality environment for the rest of the community

  Background:
    Given a user with username "adminuser" and password "AdminPassword123!" exists
    And the user "adminuser" has the "Admin" role
    And a user with username "malicioususer" and password "BadPassword123!" exists
    And the user "malicioususer" has the "User" role

  Scenario: Admin can see the Ban button for an active user
    When I log in with username "adminuser" and password "AdminPassword123!"
    And I am authenticated
    And I navigate to the Admin page
    Then I should see a "Ban" button for user "malicioususer"

  Scenario: Admin can successfully ban a user with a reason
    When I log in with username "adminuser" and password "AdminPassword123!"
    And I am authenticated
    And I navigate to the Admin page
    And I click "Ban" for user "malicioususer"
    Then I should see a ban confirmation modal for "malicioususer"
    When I enter "Spamming the system" as the ban reason
    And I confirm the ban
    Then I should see "Unban" instead of "Ban" for user "malicioususer"
    And a moderation action should be logged for "Banned" "malicioususer"

  Scenario: Banned user cannot log in and sees specific error message
    Given "malicioususer" is banned for "Rules violation"
    When I log in with username "malicioususer" and password "BadPassword123!"
    Then I should see an error message "Account Suspended"
    And I should not be authenticated

  Scenario: Banned user session is invalidated
    When I log in with username "malicioususer" and password "BadPassword123!"
    And I am authenticated
    And "adminuser" bans "malicioususer" for "Policy violation"
    And I navigate to the Dashboard page
    Then I should be redirected to the Login page
    When I log in with username "malicioususer" and password "BadPassword123!"
    Then I should see an error message "Account Suspended"
    And I should not be authenticated

  Scenario: Admin can unban a banned user
    Given "malicioususer" is banned for "Mistake"
    When I log in with username "adminuser" and password "AdminPassword123!"
    And I am authenticated
    And I navigate to the Admin page
    And I click "Unban" for user "malicioususer"
    Then I should see a "Ban" button for user "malicioususer"
    And a moderation action should be logged for "Unbanned" "malicioususer"
