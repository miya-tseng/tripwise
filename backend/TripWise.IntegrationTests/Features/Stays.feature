Feature: Stays
  As a traveler
  I want to record where I sleep each night
  So that every day shows my hotel and its cost is in the budget

  Background:
    Given a trip "Tokyo" from 2021-05-01 to 2021-05-04 exists

  Scenario: Add a stay
    When I add a stay "Hotel Gracery" from 2021-05-01 to 2021-05-03 costing 9000 TWD
    Then the response status is 201
    And the response stay "Hotel Gracery" has 2 nights

  Scenario: Each night in the schedule shows its stay
    Given the trip has a stay "Hotel Gracery" from 2021-05-01 to 2021-05-03 costing 9000 TWD
    And the trip has a stay "Ryokan" from 2021-05-03 to 2021-05-05 costing 20000 TWD
    Then the nights in the schedule are:
      | date       | stay          | checkingOut   |
      | 2021-05-01 | Hotel Gracery |               |
      | 2021-05-02 | Hotel Gracery |               |
      | 2021-05-03 | Ryokan        | Hotel Gracery |
      | 2021-05-04 | Ryokan        |               |

  Scenario: Stay costs are split across nights and counted in the budget
    Given the trip has an activity "Senso-ji" on 2021-05-01 from 10:00 to 11:00 costing 500
    And the trip has a stay "Hotel Gracery" from 2021-05-01 to 2021-05-03 costing 9000 TWD
    Then the schedule total is 9500
    And the schedule total on 2021-05-01 is 5000
    And the schedule total on 2021-05-02 is 4500

  Scenario: Stays in another currency are converted
    Given the trip has a stay "Ryokan" from 2021-05-01 to 2021-05-02 costing 20000 JPY
    Then the schedule lists the stay "Ryokan" costing 20000 JPY converted to TWD

  Scenario: Stays cannot overlap
    Given the trip has a stay "Hotel Gracery" from 2021-05-01 to 2021-05-03 costing 9000 TWD
    When I add a stay "Ryokan" from 2021-05-02 to 2021-05-04 costing 20000 TWD
    Then the response status is 409
    And the error detail is "These nights overlap with \"Hotel Gracery\" (2021-05-01 to 2021-05-03)."

  Scenario: Stays must be within the trip
    When I add a stay "Hotel" from 2021-04-30 to 2021-05-02 costing 0 TWD
    Then the response status is 400
    And the error detail is "Stay dates must be within the trip dates."

  Scenario: Check-out must be after check-in
    When I add a stay "Hotel" from 2021-05-02 to 2021-05-02 costing 0 TWD
    Then the response status is 400
    And the error detail is "Check-out must be after check-in."

  Scenario: Add a stay from a place
    When I add a stay at the place "fake-tokyo-tower" from 2021-05-01 to 2021-05-02
    Then the response status is 201
    And the schedule lists the stay "Tokyo Tower" at "4 Chome-2-8 Shibakoen, Minato City, Tokyo, Japan"

  Scenario: Update a stay
    Given the trip has a stay "Hotel" from 2021-05-01 to 2021-05-02 costing 0 TWD
    When I change the stay "Hotel" to "Hotel Gracery" from 2021-05-01 to 2021-05-04 as "Booked"
    Then the response status is 200
    And the response stay "Hotel Gracery" has 3 nights
    And the schedule lists the stay "Hotel Gracery" as "Booked"

  Scenario: Delete a stay
    Given the trip has a stay "Hotel" from 2021-05-01 to 2021-05-02 costing 0 TWD
    When I delete the stay "Hotel"
    Then the response status is 204
    And the schedule has no stays

  Scenario: Delete an unknown stay
    When I delete an unknown stay
    Then the response status is 404

  Scenario: Shortening the trip past a stay is rejected
    Given the trip has a stay "Ryokan" from 2021-05-04 to 2021-05-05 costing 0 TWD
    When I update the trip to "Tokyo" from 2021-05-01 to 2021-05-02
    Then the response status is 400
    And the error detail is "A stay falls outside the new dates. Change or delete it first."
