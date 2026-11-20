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
