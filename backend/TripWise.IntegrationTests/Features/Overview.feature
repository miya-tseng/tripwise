Feature: Trip overview
  As a traveler
  I want an overview of my trip
  So that I can see at a glance what is planned, what is missing and where the money goes

  Background:
    Given a trip "Tokyo" from 2021-05-01 to 2021-05-03 exists

  Scenario: Overview of a planned trip
    Given the trip has a "Food" activity "Sushi" on 2021-05-01 from 07:00 to 08:00 costing 4800 JPY with booking "ToBook"
    And the trip has a "Sightseeing" activity "Senso-ji" on 2021-05-01 from 10:00 to 11:00 costing 500 TWD with booking "Booked"
    And the trip has a stay "Hotel" from 2021-05-01 to 2021-05-03 costing 9000 TWD
    When I get the trip overview
    Then the response status is 200
    And the overview has 3 days, 2 activities and 2 of 3 nights with a stay
    And the overview spend is 10500 in total, 1500 on activities and 9000 on stays
    And the overview average per day is 3500
    And the overview spend by category is "Food 1000, Sightseeing 500"
    And the overview day totals are "6000, 4500, 0"
    And the overview booking progress is 1 to book, 1 booked and 0 paid
    And the overview top expenses are "Hotel, Sushi, Senso-ji"
    And the overview needs attention for "ActivityToBook Sushi, NightWithoutStay 2021-05-03, EmptyDay 2021-05-02, EmptyDay 2021-05-03"

  Scenario: Overview of an empty trip
    When I get the trip overview
    Then the response status is 200
    And the overview spend is 0 in total, 0 on activities and 0 on stays
    And the overview needs attention for "NightWithoutStay 2021-05-01, NightWithoutStay 2021-05-02, NightWithoutStay 2021-05-03, EmptyDay 2021-05-01, EmptyDay 2021-05-02, EmptyDay 2021-05-03"

  Scenario: Overview without exchange rates
    Given the trip has an activity "Sushi" on 2021-05-01 from 07:00 to 08:00 costing 4800 JPY
    And the exchange rates are unavailable
    When I get the trip overview
    Then the response status is 200
    And the overview has no totals in TWD

  Scenario: Overview of an unknown trip
    When I get the overview of an unknown trip
    Then the response status is 404
