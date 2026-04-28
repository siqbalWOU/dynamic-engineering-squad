Feature: Points Shop
  As a signed-in user
  I want to browse and purchase points shop items
  So that I can unlock dashboard cosmetics

  Scenario: Points Shop page loads for an authenticated user
    Given a points shop user "shop-page-user" with password "Password123!" and current balance 20 exists
    When I sign in to the points shop as "shop-page-user" with password "Password123!"
    And I open the Points Shop page
    Then the Points Shop page should load successfully

  Scenario: Points Shop item details and current balance are visible
    Given a points shop user "shop-details-user" with password "Password123!" and current balance 25 exists
    When I sign in to the points shop as "shop-details-user" with password "Password123!"
    And I open the Points Shop page
    Then the points shop should show a current balance of 25
    And the points shop should show the item "Safety Wave Background"
    And the points shop item "Safety Wave Background" should show description "Sweep your dashboard card with dark contour waves and safety orange-yellow highlights."
    And the points shop item "Safety Wave Background" should show cost 10

  Scenario: Successful purchase when the user has enough points
    Given a points shop user "shop-rich-user" with password "Password123!" and current balance 10 exists
    When I sign in to the points shop as "shop-rich-user" with password "Password123!"
    And I open the Points Shop page
    And I purchase the points shop item "Safety Wave Background"
    Then the points shop should show a success message containing "Purchased Safety Wave Background for 10 points."
    And the points shop should show a current balance of 0
    And the user "shop-rich-user" should own the points shop item "Safety Wave Background"

  Scenario: Purchase fails when the user has insufficient points
    Given a points shop user "shop-poor-user" with password "Password123!" and current balance 5 exists
    When I sign in to the points shop as "shop-poor-user" with password "Password123!"
    And I open the Points Shop page
    And I attempt to purchase the points shop item "Safety Wave Background"
    Then the points shop should show an error message containing "You do not have enough points for that item."
    And the user "shop-poor-user" should not own the points shop item "Safety Wave Background"
    And the points shop item "Safety Wave Background" should be unavailable for purchase

  Scenario: Single-purchase item cannot be purchased twice
    Given a points shop user "shop-owned-user" with password "Password123!" and current balance 20 exists
    And the user "shop-owned-user" already owns the points shop item "Safety Wave Background"
    When I sign in to the points shop as "shop-owned-user" with password "Password123!"
    And I open the Points Shop page
    And I attempt to purchase the points shop item "Safety Wave Background"
    Then the points shop should show an error message containing "You already own that item."
    And the user "shop-owned-user" should have exactly 1 purchase record for the points shop item "Safety Wave Background"
    And the points shop item "Safety Wave Background" should be marked as owned
