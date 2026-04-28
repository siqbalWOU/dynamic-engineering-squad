@ignore
@ImageSeverity
Feature: Image Severity Estimation
  As a user
  I want uploaded damage images to be classified by severity
  So that I can understand how serious the reported issue is

# NonParallelizable makes it so scenarios don't run at the same time as other tests and prevents race conditions

  @NonParallelizable
  Scenario: Successful image severity estimation shows severity on the details page
    Given image moderation passes
    And image severity estimation succeeds with status "High" and reason "Large pothole with deep cracking"
    And I am on the Report Issue page for image severity testing
    When I submit a report with description "Large pothole near campus" and a valid test image
    Then I should be redirected to the report details page
    And I should see severity status "High"
    And I should see severity reason containing "Large pothole with deep cracking"

  @NonParallelizable
  Scenario: Failed image severity estimation leaves severity as Pending
    Given image moderation passes
    And image severity estimation fails
    And I am on the Report Issue page for image severity testing
    When I submit a report with description "Cracked sidewalk near library" and a valid test image
    Then I should be redirected to the report details page
    And I should see severity status "Pending"

  @NonParallelizable
  Scenario: Rejected image moderation prevents submission
    Given image moderation rejects the uploaded image
    And I am on the Report Issue page for image severity testing
    When I submit a report with description "Bad image upload test" and a valid test image
    Then I should remain on the Report Issue page
    And I should see an image moderation error message