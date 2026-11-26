Feature: Trips
  As a traveler
  I want to create, view and delete trips
  So that I can organize my journeys

  Scenario: Create a trip
    When I create a trip "Tokyo" from 2021-05-01 to 2021-05-05
    Then the response status is 201
    And the response is a trip "Tokyo" from 2021-05-01 to 2021-05-05 with 5 days
    And the response location points to the trip

  Scenario: Create a trip without a name
    When I create a trip without a name from 2021-05-01 to 2021-05-01
    Then the response status is 400

  Scenario: Create a trip longer than 30 days
    When I create a trip "Long" from 2021-05-01 to 2021-05-31
    Then the response status is 400
    And the error detail is "A trip cannot be longer than 30 days."

  Scenario: Get an existing trip
    Given a trip "Osaka" from 2021-05-01 to 2021-05-03 exists
    When I get the trip
    Then the response status is 200
    And the response is a trip "Osaka" from 2021-05-01 to 2021-05-03 with 3 days

  Scenario: Get an unknown trip
    When I get an unknown trip
    Then the response status is 404

  Scenario: List trips
    Given a trip "Kyoto" from 2021-05-01 to 2021-05-03 exists
    When I list all trips
    Then the response status is 200
    And the list contains the trip

  Scenario: Delete an existing trip
    Given a trip "Nara" from 2021-05-01 to 2021-05-03 exists
    When I delete the trip
    Then the response status is 204
    And the trip no longer exists

  Scenario: Delete an unknown trip
    When I delete an unknown trip
    Then the response status is 404

  Scenario: Update a trip
    Given a trip "Tokyo" from 2021-05-01 to 2021-05-03 exists
    When I update the trip to "  Osaka  " from 2021-05-02 to 2021-05-06
    Then the response status is 200
    And the response is a trip "Osaka" from 2021-05-02 to 2021-05-06 with 5 days
    And the stored trip is "Osaka" from 2021-05-02 to 2021-05-06

  Scenario: Extending a trip adds empty days to the schedule
    Given a trip "Tokyo" from 2021-05-01 to 2021-05-02 exists
    And the trip has an activity "Senso-ji" on 2021-05-01 from 10:00 to 11:00
    When I update the trip to "Tokyo" from 2021-05-01 to 2021-05-03
    Then the response status is 200
    And the schedule on 2021-05-01 is "Senso-ji"
    And the schedule on 2021-05-03 is ""

  Scenario: Shrinking a trip with activities outside the new dates
    Given a trip "Tokyo" from 2021-05-01 to 2021-05-03 exists
    And the trip has an activity "Senso-ji" on 2021-05-01 from 10:00 to 11:00
    And the trip has an activity "Tokyo Tower" on 2021-05-03 from 18:00 to 19:00
    When I update the trip to "Osaka" from 2021-05-01 to 2021-05-02
    Then the response status is 409
    And the error detail is "1 activity falls outside the new dates. Move or delete it first."
    And the stored trip is "Tokyo" from 2021-05-01 to 2021-05-03
    And the schedule on 2021-05-03 is "Tokyo Tower"

  Scenario: Shrinking a trip when no activity is outside the new dates
    Given a trip "Tokyo" from 2021-05-01 to 2021-05-03 exists
    And the trip has an activity "Senso-ji" on 2021-05-02 from 10:00 to 11:00
    When I update the trip to "Tokyo" from 2021-05-02 to 2021-05-02
    Then the response status is 200
    And the response is a trip "Tokyo" from 2021-05-02 to 2021-05-02 with 1 days

  Scenario: Update a trip without a name
    Given a trip "Tokyo" from 2021-05-01 to 2021-05-03 exists
    When I update the trip to "   " from 2021-05-01 to 2021-05-03
    Then the response status is 400
    And the stored trip is "Tokyo" from 2021-05-01 to 2021-05-03

  Scenario: Update a trip to be longer than 30 days
    Given a trip "Tokyo" from 2021-05-01 to 2021-05-03 exists
    When I update the trip to "Tokyo" from 2021-05-01 to 2021-05-31
    Then the response status is 400
    And the error detail is "A trip cannot be longer than 30 days."

  Scenario: Update an unknown trip
    When I update an unknown trip to "Osaka" from 2021-05-01 to 2021-05-02
    Then the response status is 404
