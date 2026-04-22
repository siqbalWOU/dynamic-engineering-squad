Feature: Avatar Upload

    Scenario: User uploads a valid PNG file
        Given a registered user exists
        When they upload a valid PNG file under 5MB
        Then the avatar should be saved successfully
        And the user AvatarUrl should start with "/uploads/avatars/"
        And the user AvatarKey should be null


    Scenario: User uploads an invalid file type
        Given a registered user exists
        When they upload a GIF file
        Then the upload should fail
        And the avatar error message should be "Only JPG and PNG files are accepted."

    Scenario: from uploaded photo back to a preset avatar
        Given a registered user exists with an uploaded photo
        When they select a preset avatar key 
        Then the avatar should be saved successfully
        And the user AvatarKey should be set to the selected key
        And the user AvatarUrl should be null    

