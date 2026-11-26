Feature: Places
  As a traveler
  I want to search for real places when adding an activity
  So that every activity points to an actual location

  Scenario: Search places
    When I search places for "senso"
    Then the response status is 200
    And the place suggestions are "Senso-ji"

  Scenario: Search places matches the address
    When I search places for "Kyoto"
    Then the place suggestions are "Fushimi Inari Taisha, Kiyomizu-dera"

  Scenario: Blank search returns nothing
    When I search places for "   "
    Then the response status is 200
    And there are no place suggestions

  Scenario: Search text is too long
    When I search places with 101 characters
    Then the response status is 400
    And the error detail is "Search text cannot exceed 100 characters."

  Scenario: Place search is unavailable
    Given place search is unavailable
    When I search places for "senso"
    Then the response status is 502
    And the error detail is "Place search is unavailable right now."

  Scenario: Get place details
    When I get the place "fake-senso-ji"
    Then the response status is 200
    And the place is "Senso-ji" at "2 Chome-3-1 Asakusa, Taito City, Tokyo, Japan"

  Scenario: Get an unknown place
    When I get the place "unknown-place"
    Then the response status is 404

  Scenario: Add an activity for a place
    Given a trip "Tokyo" from 2021-05-01 to 2021-05-03 exists
    When I add the place "fake-senso-ji" on 2021-05-01 from 10:00 to 11:00
    Then the response status is 201
    And the response activity is "Senso-ji" at the place "fake-senso-ji"
    And the activity "Senso-ji" is at the place "fake-senso-ji" in the schedule

  Scenario: Add an activity for an unknown place
    Given a trip "Tokyo" from 2021-05-01 to 2021-05-03 exists
    When I add the place "unknown-place" on 2021-05-01 from 10:00 to 11:00
    Then the response status is 400
    And the error detail is "Place not found."
    And the schedule on 2021-05-01 is ""

  Scenario: Add an activity for a place while place search is unavailable
    Given a trip "Tokyo" from 2021-05-01 to 2021-05-03 exists
    And place search is unavailable
    When I add the place "fake-senso-ji" on 2021-05-01 from 10:00 to 11:00
    Then the response status is 502
    And the schedule on 2021-05-01 is ""

  Scenario: Editing an activity keeps its place without searching again
    Given a trip "Tokyo" from 2021-05-01 to 2021-05-03 exists
    And the trip has the place "fake-senso-ji" on 2021-05-01 from 10:00 to 11:00
    When I move the place activity "Senso-ji" to 13:00 to 14:00
    Then the response status is 200
    And the activity "Senso-ji" is at the place "fake-senso-ji" in the schedule
    And place details were requested 1 time

  Scenario: Activities without a place still work
    Given a trip "Tokyo" from 2021-05-01 to 2021-05-03 exists
    When I add an activity "Free time" on 2021-05-01 from 15:00 to 16:00
    Then the response status is 201
    And the activity "Free time" has no place in the schedule
