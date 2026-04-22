Feature: Email Verification
    As a newly registered user
    I want to receive an automated confirmation email with a verification link
    So that I can prove I actually own this inbox and unlock full access to the application

    Scenario: Successful registration triggers confirmation email
        Given I am on the registration page
        When I submit a valid registration form with username "newuser" and email "new@example.com"
        Then I should be redirected to the registration confirmation page
        And a confirmation email should be sent to "new@example.com"

    Scenario: Unconfirmed user cannot login
        Given a user "unconfirmed" with email "unconfirmed@example.com" exists but is not confirmed
        And I am on the login page
        When I attempt to login with username "unconfirmed" and password "Password123!"
        Then I should see an error message "You must confirm your email before logging in."
        And I should see a button to resend the verification email

    Scenario: Successful email confirmation via link
        Given a user "verify-me" with a valid confirmation token exists
        When I navigate to the confirmation link
        Then I should see the email confirmed success message
        And the user "verify-me" should be marked as confirmed in the database

    Scenario: Resending the verification email from login page
        Given a user "resend-user" with email "resend@example.com" exists but is not confirmed
        And I attempted to login as "resend-user" and saw the resend button
        When I click the resend verification email button
        Then I should see a message "Verification email sent. Please check your inbox."
        And a new confirmation email should be sent to "resend@example.com"
