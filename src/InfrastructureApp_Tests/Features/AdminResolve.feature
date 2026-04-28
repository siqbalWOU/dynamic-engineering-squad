Feature: Admin Resolve and Community Fix Verification
  As a community member or admin
  I want to verify that reported issues have been fixed
  So that the community can track resolution of infrastructure problems

  Scenario: Verify button appears on Approved report details page
    Given an approved report exists with description "Pothole on Oak Street"
    When I navigate to that approved report's details page
    Then the details page should contain "I've verified this is fixed"

  Scenario: Verify count starts at zero on a new report
    Given an approved report exists with description "Broken bench in park"
    When I navigate to that approved report's details page
    Then the details page should contain "0"

  Scenario: Verify status endpoint returns valid JSON for a report
    Given an approved report exists with description "Cracked pavement near library"
    When I request the verify status for that report
    Then the verify status response should be 200 OK
    And the verify status should contain "verifyCount"
    And the verify status should contain "userHasVerified"

  Scenario: Verify fixes page is accessible
    When I navigate to the Verify Fixes page
    Then the verify fixes page should load successfully

  Scenario: Mark as Resolved endpoint requires authentication
    Given an approved report exists with description "Flooded road near school"
    When I post to mark that report as resolved without being logged in
    Then I should be redirected to login

  Scenario: Mark as Verified Fixed endpoint requires authentication
    Given a resolved report exists with description "Fixed pothole on Main Street"
    When I post to mark that report as verified fixed without being logged in
    Then I should be redirected to login
