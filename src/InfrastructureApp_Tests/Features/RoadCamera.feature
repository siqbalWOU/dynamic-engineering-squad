Feature: Road Camera

    Scenario: Cameras are availablee and displayed
        Given the TripCheck API returns a list of cameras
        When a user visits the Road Camera index page
        Then the view model should contain cameras
        And no error message should be shown

    Scenario: No cameras returned from API
        Given the TripCheck API returns no cameras
        When a user visits the Road Camera index page
        Then the view model should contain no cameras
        And the camera error message should be "Road camera data is temporarily unavailable."

    Scenario: User views details for a valid camera
        Given the TripCheck API returns a camera with id "CAM001"
        When a user views the details for camera "CAM001"
        Then the details view model should contain the camera
        And no error message should be shown 

    Scenario: User views details for an invalid camera
        Given the TripCheck API returns no camera with id "INVALID"
        When a user views the details for camera "INVALID"
        Then the details view model camera should be null
        And the camera error message should be "Road camera data is temporarily unavailable."

    Scenario: Refresh image returns data for a valid camera
        Given the TripCheck API returns a camera with id "CAM001"
        When the image is refreshed for camera "CAM001"
        Then the refresh response should contain a cameraId
        And the refresh response should contain an imageUrl

    Scenario: Refresh image returns error for an invalid camera
        Given the TripCheck API returns no camera with id "INVALID"
        When the image is refreshed for camera "INVALID"
        Then the refresh response should contain an error            