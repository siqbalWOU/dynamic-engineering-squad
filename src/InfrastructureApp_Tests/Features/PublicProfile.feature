Feature: Public User Profile
  As a User
  I want to view another user's public profile and see a list of the infrastructure issues they have reported
  So that I can track community contributions and avoid submitting duplicate reports

  Scenario: A user can access another user's public profile from the leaderboard
    Given a user "ContributorUser" exists with 5 approved reports
    And I am on the Leaderboard page
    When I click on the username "ContributorUser"
    Then I should be redirected to "ContributorUser"'s public profile page
    And I should see "ContributorUser"'s username
    And I should see 5 reports listed in the contribution feed

  Scenario: Public profile shows issue title and date
    Given a user "ContributorUser" exists with a report titled "Broken Sidewalk" on "2026-05-14"
    When I navigate to "ContributorUser"'s public profile page
    Then I should see a report with title "Broken Sidewalk" and date "2026-05-14"

  Scenario: Clicking a report title redirects to details
    Given a user "ContributorUser" exists with a report titled "Broken Sidewalk"
    When I navigate to "ContributorUser"'s public profile page
    And I click on the report title "Broken Sidewalk"
    Then I should be redirected to the full details page for that report

  Scenario: Public profile restricts sensitive information
    Given a user "ContributorUser" exists with email "private@example.com"
    When I navigate to "ContributorUser"'s public profile page
    Then I should not see "private@example.com"
    And I should not see account settings links

  Scenario: Public profile shows empty state for user with no reports
    Given a user "NewUser" exists with no reports
    When I navigate to "NewUser"'s public profile page
    Then I should see "This user hasn't reported any infrastructure issues yet."

  Scenario: Contribution list is paginated
    Given a user "ProlificUser" exists with 15 approved reports
    When I navigate to "ProlificUser"'s public profile page
    Then I should see the first 10 reports
    And I should see pagination controls
