Feature: Forgot Password
  As a forgetful user
  I want to securely request a password reset link via my registered email
  So that I can regain access to my account

  Background:
    Given a user with username "forgetfuluser" and password "OldPassword123!" exists
    And the user's email is confirmed

  Scenario: User can see the Forgot Password link on the Login page
    When I navigate to the Login page
    Then I should see a "Forgot Password" link

  Scenario: Submitting an email triggers a recovery email
    When I navigate to the Login page
    And I click the "Forgot Password" link
    And I enter "forgetfuluser@example.com" as my email address
    And I click "Send Reset Link"
    Then I should see a message "If your email is in our system, you will receive a reset link shortly."
    And a password reset email should be sent to "forgetfuluser@example.com"

  Scenario: Resetting password with a valid link
    Given a valid password reset token for user "forgetfuluser"
    When I navigate to the Reset Password page with the valid token
    And I enter "NewPassword123!" as my new password
    And I confirm "NewPassword123!" as my new password
    And I click "Reset Password"
    Then I should see a message "Your password has been reset successfully."
    When I log in with username "forgetfuluser" and password "NewPassword123!"
    Then I should be authenticated

  Scenario: Resetting password with an invalid or expired token
    When I navigate to the Reset Password page with an invalid token
    Then I should see an error message "Invalid or expired token."
