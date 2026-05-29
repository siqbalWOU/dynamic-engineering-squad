Feature: Dashboard Submitted Reports
  As a logged-in user
  I want to see reports I submitted on my Dashboard
  So that I can privately track my report activity in one place

  Scenario: Logged-in user sees their submitted reports on the Dashboard
    Given I am logged in as a Dashboard user
    And I have submitted a report with description "Broken sidewalk near library"
    When I visit my Dashboard
    Then I should see "My Submitted Reports"
    And I should see my submitted report with description "Broken sidewalk near library"

  Scenario: Logged-in user does not see reports submitted by another user
    Given I am logged in as a Dashboard user
    And another user submitted a report with description "Streetlight outage on Pine"
    When I visit my Dashboard
    Then I should see "My Submitted Reports"
    And I should not see a submitted report with description "Streetlight outage on Pine"
