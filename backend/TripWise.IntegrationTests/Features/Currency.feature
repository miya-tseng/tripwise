Feature: Currency
  As a traveler
  I want to record each cost in its own currency and see budgets converted to TWD
  So that I know how much the trip costs in my home currency

  Background:
    Given a trip "Tokyo" from 2021-05-01 to 2021-05-03 exists

  Scenario: Add an activity with a foreign currency
    When I add an activity "Shibuya Sky" on 2021-05-01 from 17:00 to 18:00 costing 2200 jpy
    Then the response status is 201
    And the response activity costs 2200 JPY worth 458.33 TWD
    And the activity "Shibuya Sky" costs 2200 JPY worth 458.33 TWD in the schedule

  Scenario: Costs without a currency are in TWD
    When I add an activity "Senso-ji" on 2021-05-01 from 10:00 to 11:00 costing 150
    Then the response status is 201
    And the response activity costs 150 TWD worth 150 TWD

  Scenario: Schedule converts every currency to TWD
    Given the trip has an activity "Tsukiji" on 2021-05-01 from 07:00 to 08:00 costing 3000 JPY
    And the trip has an activity "Senso-ji" on 2021-05-01 from 10:00 to 11:00 costing 150
    And the trip has an activity "Airport bus" on 2021-05-02 from 13:00 to 15:00 costing 10 USD
    When I get the schedule
    Then the response status is 200
    And the schedule total on 2021-05-01 is 775
    And the schedule total on 2021-05-02 is 312.50
    And the schedule total is 1087.50
    And the schedule totals per currency are "150 TWD, 3000 JPY, 10 USD"
    And the exchange rates in the schedule are available

  Scenario: Changing the currency of an activity
    Given the trip has an activity "Shibuya Sky" on 2021-05-01 from 17:00 to 18:00 costing 2200 JPY
    When I change the cost of "Shibuya Sky" to 20 USD
    Then the response status is 200
    And the response activity costs 20 USD worth 625 TWD
    And the schedule total is 625

  Scenario: Unsupported currency
    When I add an activity "Senso-ji" on 2021-05-01 from 10:00 to 11:00 costing 10 XYZ
    Then the response status is 400
    And the error detail is "Currency XYZ is not supported."

  Scenario: Invalid currency code
    When I add an activity "Senso-ji" on 2021-05-01 from 10:00 to 11:00 costing 10 JP
    Then the response status is 400
    And the error detail is "Currency must be a 3-letter ISO 4217 code."

  Scenario: Schedule still loads when exchange rates are unavailable
    Given the trip has an activity "Tsukiji" on 2021-05-01 from 07:00 to 08:00 costing 3000 JPY
    And the exchange rates are unavailable
    When I get the schedule
    Then the response status is 200
    And the schedule has no TWD total
    And the schedule totals per currency are "3000 JPY"
    And the activity "Tsukiji" costs 3000 JPY in the schedule

  Scenario: Latest exchange rates
    When I get the exchange rates
    Then the response status is 200
    And the exchange rates are available with "TWD, JPY, USD"

  Scenario: Exchange rates are unavailable
    Given the exchange rates are unavailable
    When I get the exchange rates
    Then the response status is 200
    And the exchange rates are unavailable with only TWD
