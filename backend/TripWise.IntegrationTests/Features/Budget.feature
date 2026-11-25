Feature: Budget
  As a traveler
  I want to set a cost for each activity and see daily and total budgets
  So that I can control my travel expenses

  Background:
    Given a trip "Tokyo" from 2021-05-01 to 2021-05-03 exists

  Scenario: Add an activity with a cost
    When I add an activity "Shibuya Sky" on 2021-05-01 from 17:00 to 18:00 costing 2200.50
    Then the response status is 201
    And the response activity costs 2200.50
    And the activity "Shibuya Sky" costs 2200.50 in the schedule

  Scenario: Add an activity without a cost
    When I add an activity "Senso-ji" on 2021-05-01 from 10:00 to 11:00
    Then the response status is 201
    And the response activity costs 0

  Scenario: Add an activity with a negative cost
    When I add an activity "Shibuya Sky" on 2021-05-01 from 17:00 to 18:00 costing -1
    Then the response status is 400
    And the error detail is "Amount cannot be negative."
    And the schedule total is 0

  Scenario: Add an activity with more than two decimal places
    When I add an activity "Shibuya Sky" on 2021-05-01 from 17:00 to 18:00 costing 12.345
    Then the response status is 400
    And the error detail is "Amount cannot have more than 2 decimal places."

  Scenario: Update the cost of an activity
    Given the trip has an activity "Shibuya Sky" on 2021-05-01 from 17:00 to 18:00 costing 2200
    When I change the cost of "Shibuya Sky" to 2500
    Then the response status is 200
    And the response activity costs 2500
    And the activity "Shibuya Sky" costs 2500 in the schedule
    And the schedule total is 2500

  Scenario: Schedule shows daily subtotals and the trip total
    Given the trip has an activity "Tsukiji" on 2021-05-01 from 07:00 to 08:00 costing 1500.50
    And the trip has an activity "Senso-ji" on 2021-05-01 from 10:00 to 11:00
    And the trip has an activity "Shibuya Sky" on 2021-05-01 from 17:00 to 18:00 costing 2200
    And the trip has an activity "teamLab" on 2021-05-03 from 13:00 to 15:00 costing 3800.25
    When I get the schedule
    Then the response status is 200
    And the schedule total on 2021-05-01 is 3700.50
    And the schedule total on 2021-05-02 is 0
    And the schedule total on 2021-05-03 is 3800.25
    And the schedule total is 7500.75

  Scenario: Totals keep decimal precision
    Given the trip has an activity "Coffee" on 2021-05-01 from 08:00 to 08:30 costing 0.10
    And the trip has an activity "Snack" on 2021-05-01 from 09:00 to 09:30 costing 0.20
    Then the schedule total is 0.30

  Scenario: Deleting an activity removes its cost from the totals
    Given the trip has an activity "Shibuya Sky" on 2021-05-01 from 17:00 to 18:00 costing 2200
    And the trip has an activity "teamLab" on 2021-05-03 from 13:00 to 15:00 costing 3800
    When I delete the activity "Shibuya Sky"
    Then the response status is 204
    And the schedule total on 2021-05-01 is 0
    And the schedule total is 3800
