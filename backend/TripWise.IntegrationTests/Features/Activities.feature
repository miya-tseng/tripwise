Feature: Activities
  As a traveler
  I want to add, edit and delete activities for each day of my trip
  So that I can plan a realistic daily schedule

  Background:
    Given a trip "Tokyo" from 2021-05-01 to 2021-05-03 exists

  Scenario: Add an activity
    When I add an activity "Senso-ji" on 2021-05-01 from 10:00 to 11:00
    Then the response status is 201
    And the response is an activity "Senso-ji" on 2021-05-01 from 10:00 to 11:00
    And the response location points to the schedule

  Scenario: Add an activity with times without seconds
    When I add an activity "Senso-ji" on 2021-05-01 with raw times "10:00" and "11:00"
    Then the response status is 201
    And the response is an activity "Senso-ji" on 2021-05-01 from 10:00 to 11:00

  Scenario: Add an activity to an unknown trip
    When I add an activity "Senso-ji" on 2021-05-01 from 10:00 to 11:00 to an unknown trip
    Then the response status is 404

  Scenario: Add an activity without times
    When I add an activity "Senso-ji" on 2021-05-01 without times
    Then the response status is 400

  Scenario: Add an activity outside the trip dates
    When I add an activity "Senso-ji" on 2021-05-04 from 10:00 to 11:00
    Then the response status is 400
    And the error detail is "Activity date must be within the trip dates."

  Scenario: Add an activity that ends before it starts
    When I add an activity "Senso-ji" on 2021-05-01 from 11:00 to 10:00
    Then the response status is 400
    And the error detail is "End time must be later than start time."

  Scenario: Add an overlapping activity
    Given the trip has an activity "Senso-ji" on 2021-05-01 from 10:00 to 11:00
    When I add an activity "Ueno Park" on 2021-05-01 from 10:30 to 11:30
    Then the response status is 409
    And the error detail is "(10:00-11:00) on 2021-05-01."
    And the schedule on 2021-05-01 is "Senso-ji"

  Scenario: Add a back-to-back activity
    Given the trip has an activity "Senso-ji" on 2021-05-01 from 10:00 to 11:00
    When I add an activity "Ueno Park" on 2021-05-01 from 11:00 to 12:00
    Then the response status is 201
    And the schedule on 2021-05-01 is "Senso-ji, Ueno Park"

  Scenario: Add an activity at the same time on another day
    Given the trip has an activity "Senso-ji" on 2021-05-01 from 10:00 to 11:00
    When I add an activity "Ueno Park" on 2021-05-02 from 10:00 to 11:00
    Then the response status is 201

  Scenario: Update an activity
    Given the trip has an activity "Senso-ji" on 2021-05-01 from 10:00 to 11:00
    When I update the activity "Senso-ji" to "Tokyo Tower" on 2021-05-02 from 14:00 to 15:30
    Then the response status is 200
    And the response is an activity "Tokyo Tower" on 2021-05-02 from 14:00 to 15:30
    And the schedule on 2021-05-01 is ""
    And the schedule on 2021-05-02 is "Tokyo Tower"

  Scenario: Update an activity so it overlaps only itself
    Given the trip has an activity "Senso-ji" on 2021-05-01 from 10:00 to 11:00
    When I update the activity "Senso-ji" to "Senso-ji" on 2021-05-01 from 10:30 to 11:30
    Then the response status is 200

  Scenario: Update an activity so it overlaps another activity
    Given the trip has an activity "Senso-ji" on 2021-05-01 from 10:00 to 11:00
    And the trip has an activity "Ueno Park" on 2021-05-01 from 13:00 to 14:00
    When I update the activity "Ueno Park" to "Ueno Zoo" on 2021-05-01 from 10:30 to 11:30
    Then the response status is 409
    And the activity "Ueno Park" is still on 2021-05-01 from 13:00 to 14:00

  Scenario: Update an unknown activity
    When I update an unknown activity to "Senso-ji" on 2021-05-01 from 10:00 to 11:00
    Then the response status is 404

  Scenario: Delete an activity
    Given the trip has an activity "Senso-ji" on 2021-05-01 from 10:00 to 11:00
    When I delete the activity "Senso-ji"
    Then the response status is 204
    And the schedule on 2021-05-01 is ""

  Scenario: Delete an unknown activity
    When I delete an unknown activity
    Then the response status is 404

  Scenario: Schedule groups activities by day sorted by start time
    Given the trip has an activity "Shibuya" on 2021-05-01 from 15:00 to 16:00
    And the trip has an activity "Senso-ji" on 2021-05-01 from 09:00 to 10:00
    And the trip has an activity "Tokyo Tower" on 2021-05-03 from 18:00 to 19:00
    And the trip has an activity "Tsukiji" on 2021-05-01 from 07:00 to 08:00
    When I get the schedule
    Then the response status is 200
    And the schedule has the days 2021-05-01, 2021-05-02, 2021-05-03
    And the schedule on 2021-05-01 is "Tsukiji, Senso-ji, Shibuya"
    And the schedule on 2021-05-02 is ""
    And the schedule on 2021-05-03 is "Tokyo Tower"

  Scenario: Get the schedule of an unknown trip
    When I get the schedule of an unknown trip
    Then the response status is 404

  Scenario: Deleting a trip deletes its activities
    Given the trip has an activity "Senso-ji" on 2021-05-01 from 10:00 to 11:00
    When I delete the trip
    Then the response status is 204
    And no activities of the trip remain in the database
