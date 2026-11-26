Feature: Suggestions
  As a traveler
  I want suggestions for places people usually plan together with a chosen place
  So that I can build my day quickly

  Background:
    Given a trip "Tokyo" from 2021-05-01 to 2021-05-03 exists

  Scenario: Get suggestions for a place
    Given the trip has the place "fake-senso-ji" on 2021-05-01 from 10:00 to 11:30
    When I get suggestions for "Senso-ji"
    Then the response status is 200
    And the suggested places are "Tokyo Skytree, Shibuya Sky, Tsukiji Outer Market, Ueno Park, teamLab Planets TOKYO"
    And every suggestion has a reason, a duration and a Google Maps link

  Scenario: The generator gets the place and the rest of the day
    Given the trip has an activity "Breakfast" on 2021-05-01 from 07:00 to 08:00
    And the trip has the place "fake-senso-ji" on 2021-05-01 from 10:00 to 11:30
    And the trip has an activity "Dinner" on 2021-05-01 from 18:00 to 19:00
    When I get suggestions for "Senso-ji"
    Then the suggestions were asked for "Senso-ji" ending at 11:30 with "Breakfast, Dinner" planned

  Scenario: Places already in the trip are not suggested
    Given the trip has the place "fake-senso-ji" on 2021-05-01 from 10:00 to 11:30
    And the trip has the place "fake-tokyo-skytree" on 2021-05-02 from 10:00 to 11:00
    When I get suggestions for "Senso-ji"
    Then the response status is 200
    And the suggested places are "Shibuya Sky, Tsukiji Outer Market, Ueno Park, teamLab Planets TOKYO, Meiji Jingu"

  Scenario: Suggestions need a place
    Given the trip has an activity "Free time" on 2021-05-01 from 15:00 to 16:00
    When I get suggestions for "Free time"
    Then the response status is 400
    And the error detail is "Suggestions need an activity with a place."

  Scenario: Suggestions for an unknown activity
    When I get suggestions for an unknown activity
    Then the response status is 404

  Scenario: Suggestions for an unknown trip
    When I get suggestions for an unknown trip
    Then the response status is 404

  Scenario: The AI provider is unavailable
    Given the trip has the place "fake-senso-ji" on 2021-05-01 from 10:00 to 11:30
    And suggestions are unavailable
    When I get suggestions for "Senso-ji"
    Then the response status is 503
    And the error detail is "Suggestions are unavailable right now."

  Scenario: Place search is unavailable while resolving suggestions
    Given the trip has the place "fake-senso-ji" on 2021-05-01 from 10:00 to 11:30
    And place search is unavailable
    When I get suggestions for "Senso-ji"
    Then the response status is 502
