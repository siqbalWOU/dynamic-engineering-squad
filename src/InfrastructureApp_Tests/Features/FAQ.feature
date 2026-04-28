Feature: FAQ Page
  As a user
  I want to read a Frequently Asked Questions page
  So that I can understand how the app works without needing technical knowledge

  Scenario: FAQ page loads successfully
    Given I navigate to the FAQ page
    Then the response should be 200 OK
    And the page should contain "Frequently Asked Questions"

  Scenario: FAQ page is linked from the footer
    Given I navigate to the Home page
    Then the footer should contain a link to the FAQ page

  Scenario: FAQ page explains the purpose of creating an account
    Given I navigate to the FAQ page
    Then the page should contain "Why should I create an account"

  Scenario: FAQ page explains how to create an account
    Given I navigate to the FAQ page
    Then the page should contain "How do I create an account"

  Scenario: FAQ page describes password requirements in plain language
    Given I navigate to the FAQ page
    Then the page should contain "6 characters"
    And the page should contain "40 characters"

  Scenario: FAQ page states passwords must match
    Given I navigate to the FAQ page
    Then the page should contain "identical"

  Scenario: FAQ page states account is not created if validation fails
    Given I navigate to the FAQ page
    Then the page should contain "will not be created"

  Scenario: FAQ page explains when an account is successfully created
    Given I navigate to the FAQ page
    Then the page should contain "How do I know my account was successfully created"

  Scenario: FAQ page states accounts are stored securely
    Given I navigate to the FAQ page
    Then the page should contain "stored"

  Scenario: FAQ page lists all four team members
    Given I navigate to the FAQ page
    Then the page should contain "Julian"
    And the page should contain "Sunair"
    And the page should contain "Phillip"
    And the page should contain "Erin"

  Scenario: FAQ page mentions the team name
    Given I navigate to the FAQ page
    Then the page should contain "Dynamic Engineering Squad"

  Scenario: FAQ content is written without exposing technical implementation details
    Given I navigate to the FAQ page
    Then the page should not contain "DbContext"
    And the page should not contain "ApplicationDbContext"
    And the page should not contain "connection string"
