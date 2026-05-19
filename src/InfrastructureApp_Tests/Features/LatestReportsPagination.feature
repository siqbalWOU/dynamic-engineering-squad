Feature: Latest Reports Pagination
  # SCRUM-157: User-facing coverage for paginated Latest Reports browsing.
  # As a user
  # I want the Latest Reports page to split reports into smaller pages
  # So that I can browse recent reports without one long overwhelming list

  Scenario: Pagination controls appear when there are more reports than one page
    Given more than one page of latest reports exists
    When I visit the Latest Reports page
    Then I should see Latest Reports pagination controls
    And the first page should be marked as the current page

  Scenario: User can navigate to the next page of Latest Reports
    Given more than one page of latest reports exists
    When I visit the Latest Reports page
    And I go to the next Latest Reports page
    Then I should see the second page of Latest Reports
    And the second page should be marked as the current page

  Scenario: Search results preserve pagination
    Given more than one page of searchable latest reports exists
    When I search Latest Reports for "Pothole"
    Then I should see Latest Reports pagination controls
    And the pagination links should preserve the search term "Pothole"

  Scenario: Sort order works with pagination
    Given more than one page of latest reports exists
    When I sort Latest Reports by oldest first
    Then I should see the oldest Latest Reports first
    And the pagination links should preserve the oldest first sort

  Scenario: User can still open a report after navigating pages
    Given more than one page of latest reports exists
    When I visit the Latest Reports page
    And I go to the next Latest Reports page
    And I open a report from the Latest Reports list
    Then the Latest Reports modal should open for that report
