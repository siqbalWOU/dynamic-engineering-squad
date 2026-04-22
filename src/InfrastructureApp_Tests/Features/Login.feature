Feature: Login
  As a registered user
  I want to be able to log in to my account
  So that I can access personalized features

  Background:
    Given a user with username "testuser" and password "Password123!" exists
    And the user's email is confirmed

  Scenario: Successful login with valid credentials
    When I log in with username "testuser" and password "Password123!"
    Then I should be redirected to the home page
    And I should be authenticated

  Scenario: Failed login with invalid password
    When I log in with username "testuser" and password "WrongPassword"
    Then I should see an error message "Invalid login attempt."
    And I should not be authenticated

  Scenario: Failed login with non-existent user
    When I log in with username "nonexistent" and password "Password123!"
    Then I should see an error message "Invalid login attempt."
    And I should not be authenticated

  Scenario: Failed login with unconfirmed email
    Given a user with username "unconfirmeduser" and password "Password123!" exists
    And the user's email is not confirmed
    When I log in with username "unconfirmeduser" and password "Password123!"
    Then I should see an error message "You must confirm your email before logging in."
    And I should not be authenticated
