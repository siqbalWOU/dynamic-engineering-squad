Feature: Home Report Expand Collapse
  As a user
  I want to expand a recent report on the Home page
  So that I can view more report details without leaving the Home page

  # SCRUM-128:
  # Inline expand/collapse controls should be available for Home recent reports
  Scenario: Recent reports include inline expand collapse controls
    Given Home page recent reports exist for expand collapse
    When I visit the Home page for expand collapse
    Then the Home recent reports should include expand controls
    And the Home recent reports should include hidden inline details panels
