Feature: Facebook Social Sharing
  As a logged-in user
  I want to share approved infrastructure issues on Facebook
  So that I can raise community awareness about local problems

  Scenario: Share button appears on approved issue for authenticated user
    Given an approved sharing report exists with description "Pothole on Main Street"
    When an authenticated user navigates to the sharing report details page
    Then the sharing page should contain "Share this issue"
    And the sharing page should contain "fa-facebook"

  Scenario: Share button is not visible on unapproved issue for authenticated user
    Given a pending sharing report exists with description "Unverified crack in pavement"
    When an authenticated user navigates to the sharing report details page
    Then the sharing page should not contain "Share this issue"

  Scenario: Share button is not visible to unauthenticated users
    Given an approved sharing report exists with description "Flooding on River Road"
    When an unauthenticated user navigates to the sharing report details page
    Then the sharing page should not contain "Share this issue"

  Scenario: Facebook share link points to Facebook sharer
    Given an approved sharing report exists with description "Broken guardrail on highway"
    When an authenticated user navigates to the sharing report details page
    Then the sharing page should contain "facebook.com/sharer"
