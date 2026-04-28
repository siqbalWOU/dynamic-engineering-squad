Feature: Flag Post
  As a platform user
  I want to flag posts that are inappropriate or contain misinformation
  So that I can help maintain a safe, accurate community environment

  Background:
    Given a report exists with description "Pothole on Oak Street"
    And I am logged in as "testuser"
    And I navigate to that report's details page

  Scenario: User opens the reporting interface
    Then I should see a "Flag" icon
    When I click the "Flag" icon
    Then I should be presented with categories "Misinformation", "Spam", "Invalid report"

  Scenario: User submits a report successfully
    Given I have clicked the "Flag" icon
    When I select category "Misinformation"
    And I click "Submit Report"
    Then I should see a confirmation message "Thank you for your report. Our moderation team will review it shortly."
    And the reporting interface should close
    And the "Flag" icon should be disabled and show "Already Flagged"

  Scenario: User cannot flag the same post twice
    Given I have already flagged that report with category "Spam"
    Then the "Flag" icon should be disabled and show "Already Flagged"

  Scenario: User flags a report from the Latest Reports modal
    When I navigate to the Latest Reports page
    And I click on the report with description "Pothole on Oak Street"
    Then the report modal should be displayed
    And I should see a "Flag" button in the modal
    When I click the "Flag" button in the modal
    Then I should be presented with categories "Misinformation", "Spam", "Invalid report"
    When I select category "Spam"
    And I click "Submit Report"
    Then I should see a confirmation message "Thank you for your report. Our moderation team will review it shortly."
    And the reporting interface should close
    And the "Flag" button in the modal should be disabled and show "Already Flagged"
