Feature: Moderation Dashboard
  As a platform moderator
  I want to review and take action on reported posts via a centralized dashboard
  So that I can efficiently enforce community guidelines and remove content that violates our standards

  Scenario: Unauthorized users are redirected to home page
    Given I am not logged in
    When I attempt to access the moderation dashboard URL
    Then I should be redirected to the home page or login page

  Scenario: Unauthorized standard users are redirected
    Given I am logged in as "standarduser"
    And I do not have "Moderator" or "Admin" roles
    When I attempt to access the moderation dashboard URL
    Then I should be redirected to the home page or login page

  Scenario: Authorized moderators can see the dashboard link and access it
    Given I am logged in as "moduser"
    And I have the "Moderator" role
    Then I should see a "Moderation" link in the navbar
    When I click the "Moderation" link
    Then I should be on the Moderation Dashboard page
    And I should see "Moderation Dashboard" heading

  Scenario: Dashboard displays flagged posts correctly
    Given a report exists with description "Bad post content"
    And it has been flagged with category "Spam" by "user1"
    And I am logged in as a moderator
    When I navigate to the moderation dashboard
    Then I should see the report with description "Bad post content"
    And it should show 1 flag
    And the flag reason should include "Spam"

  Scenario: Dismissing a report keeps the post but removes it from the queue
    Given a report exists with description "Harmless post"
    And it has been flagged with category "Invalid report"
    And I am logged in as a moderator
    And I am on the moderation dashboard
    When I click "Dismiss Report" for the report "Harmless post"
    Then the report "Harmless post" should no longer be in the moderation queue
    And the report "Harmless post" should still exist in the system

  Scenario: Removing a post deletes it permanently
    Given a report exists with description "Toxic post"
    And it has been flagged with category "Spam"
    And I am logged in as a moderator
    And I am on the moderation dashboard
    When I click "Remove Post" for the report "Toxic post"
    Then I should see a confirmation prompt
    When I confirm the removal
    Then the report "Toxic post" should no longer exist in the system
    And a moderation action should be logged for "Removed" "Toxic post"
