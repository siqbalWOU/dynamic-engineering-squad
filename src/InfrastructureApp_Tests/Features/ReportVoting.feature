Feature: Report Community Voting
  As a community member
  I want to upvote reports I have personally seen
  So that the community can signal which issues need the most attention

  Scenario: Vote button appears on report details page
    Given a report exists with description "Cracked sidewalk on Main St"
    When I navigate to that report's details page
    Then the voting page should contain "I've seen this too"

  Scenario: Vote count starts at zero on a new report
    Given a report exists with description "Broken streetlight"
    When I navigate to that report's details page
    Then the voting page should contain "0"

  Scenario: Vote status endpoint returns valid JSON for a report
    Given a report exists with description "Pothole near school"
    When I request the vote status for that report
    Then the vote status response should be 200 OK
    And the vote status should contain "voteCount"
    And the vote status should contain "userHasVoted"

  Scenario: Latest reports page contains the vote button in the modal
    Given a report exists with description "Flooded underpass"
    When I navigate to the Latest Reports page
    Then the voting page should contain "I've seen this too"
