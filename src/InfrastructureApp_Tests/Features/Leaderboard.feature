Feature: Leaderboard Page
  As a user
  I want to view the leaderboard
  So that I can see how community members rank by their contributions

  Scenario: Leaderboard page loads successfully
    Given I navigate to the Leaderboard page
    Then the leaderboard response should be 200 OK
    And the leaderboard page should contain "Leaderboard"

  Scenario: Leaderboard shows empty state when no contributions exist
    Given no leaderboard entries exist
    When I navigate to the Leaderboard page
    Then the leaderboard page should contain "No contributions yet."

  Scenario: Leaderboard shows entries when contributions exist
    Given the following leaderboard entries exist
      | UserId | UserPoints |
      | alice  | 100        |
      | bob    | 50         |
    When I navigate to the Leaderboard page
    Then the leaderboard page should contain "alice"
    And the leaderboard page should contain "bob"

  Scenario: Leaderboard displays ranking table columns when entries exist
    Given the following leaderboard entries exist
      | UserId   | UserPoints |
      | testuser | 100        |
    When I navigate to the Leaderboard page
    Then the leaderboard page should contain "Rank"
    And the leaderboard page should contain "User"
    And the leaderboard page should contain "Points"
    And the leaderboard page should contain "Updated"

  Scenario: Leaderboard shows the top N count
    Given I navigate to the Leaderboard page
    Then the leaderboard page should contain "Top"

  Scenario: Leaderboard is accessible from the Home page
    Given I navigate to the Home page as a visitor
    Then the leaderboard home page should contain a link to the Leaderboard

  Scenario: Leaderboard shows higher scoring user above lower scoring user
    Given the following leaderboard entries exist
      | UserId | UserPoints |
      | topdog | 500        |
      | newbie | 10         |
    When I navigate to the Leaderboard page
    Then "topdog" should appear before "newbie" on the leaderboard page
