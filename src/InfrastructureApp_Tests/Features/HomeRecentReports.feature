Feature: Home Page Recent Reports
  As a user
  I want to see a small list of recent reports on the Home page
  So that I can quickly view recent issues without going to the full Latest Reports page

  Scenario: Recent Activity section appears on the Home page
    Given I am on the Home page
    Then the Recent Activity section should be displayed

  Scenario: Recent reports are displayed when reports exist
    Given recent reports exist in the system
    When I visit the Home page
    Then recent reports should be displayed on the Home page

  Scenario: Home page only shows three recent reports
    Given more than three recent reports exist in the system
    When I visit the Home page
    Then only three recent reports should be displayed

  Scenario: Friendly message is shown when no reports exist
    Given no recent reports exist in the system
    When I visit the Home page
    Then a no recent reports message should be displayed