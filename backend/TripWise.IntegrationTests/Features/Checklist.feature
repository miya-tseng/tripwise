Feature: Checklist
  As a traveler
  I want a checklist of things to book and things to pack
  So that nothing is forgotten before the trip

  Background:
    Given a trip "Tokyo" from 2021-05-01 to 2021-05-03 exists

  Scenario: Add checklist items to sections
    When I add "Passport" to "ToPack" on the checklist
    Then the response status is 201
    When I add "Travel insurance" to "ToBook" on the checklist due 2021-04-20
    Then the checklist "ToPack" section is "Passport"
    And the checklist "ToBook" section is "Travel insurance"
    And the checklist item "Travel insurance" is due 2021-04-20
    And the checklist progress is 0 of 2

  Scenario: Add an item without text
    When I add "   " to "ToPack" on the checklist
    Then the response status is 400

  Scenario: Tick and untick an item
    Given the checklist has "Passport" in "ToPack"
    When I tick "Passport" on the checklist
    Then the response status is 200
    And the checklist progress is 1 of 1
    When I untick "Passport" on the checklist
    Then the checklist progress is 0 of 1

  Scenario: Edit an item and move it to another section
    Given the checklist has "Passport" in "ToPack"
    And the checklist has "Charger" in "ToPack"
    When I change the checklist item "Passport" to "Renew passport" in "ToBook"
    Then the response status is 200
    And the checklist "ToBook" section is "Renew passport"
    And the checklist "ToPack" section is "Charger"

  Scenario: Reorder a section
    Given the checklist has "Passport" in "ToPack"
    And the checklist has "Charger" in "ToPack"
    And the checklist has "Adapter" in "ToPack"
    When I reorder the checklist "ToPack" section to "Adapter, Passport, Charger"
    Then the response status is 200
    And the checklist "ToPack" section is "Adapter, Passport, Charger"

  Scenario: Reorder with a missing item
    Given the checklist has "Passport" in "ToPack"
    And the checklist has "Charger" in "ToPack"
    When I reorder the checklist "ToPack" section to "Charger"
    Then the response status is 400
    And the error detail is "The new order must list every item of the section once."

  Scenario: Delete an item
    Given the checklist has "Passport" in "ToPack"
    When I delete "Passport" from the checklist
    Then the response status is 200
    And the checklist progress is 0 of 0

  Scenario: Delete an unknown item
    When I delete an unknown item from the checklist
    Then the response status is 404

  Scenario: Activities and stays to book appear on the checklist
    Given the trip has a "Experience" activity "teamLab" on 2021-05-02 from 10:00 to 12:00 costing 0 TWD with booking "ToBook"
    And the trip has a "Food" activity "Sushi" on 2021-05-01 from 07:00 to 08:00 costing 0 TWD with booking "Paid"
    And the trip has a "Other" activity "Walk" on 2021-05-01 from 09:00 to 10:00 costing 0 TWD with booking "NotNeeded"
    When I get the checklist
    Then the checklist bookings are "Sushi done, teamLab open"
    And the checklist progress is 1 of 2

  Scenario: Booking an activity ticks it on the checklist
    Given the trip has a "Experience" activity "teamLab" on 2021-05-02 from 10:00 to 12:00 costing 0 TWD with booking "ToBook"
    When I mark the activity "teamLab" as booked on the checklist
    Then the response status is 200
    And the checklist bookings are "teamLab done"
    And the activity "teamLab" has booking status "Booked" in the schedule

  Scenario: Changing the activity status updates the checklist
    Given the trip has a "Experience" activity "teamLab" on 2021-05-02 from 10:00 to 12:00 costing 0 TWD with booking "ToBook"
    When I change the booking status of "teamLab" to "Booked"
    And I get the checklist
    Then the checklist bookings are "teamLab done"

  Scenario: Unticking a stay sets it back to To book
    Given the trip has a stay "Hotel" from 2021-05-01 to 2021-05-03 booked
    When I mark the stay "Hotel" as not booked on the checklist
    Then the response status is 200
    And the checklist bookings are "Hotel open"

  Scenario: An activity that needs no booking cannot be ticked
    Given the trip has a "Other" activity "Walk" on 2021-05-01 from 09:00 to 10:00 costing 0 TWD with booking "NotNeeded"
    When I mark the activity "Walk" as booked on the checklist
    Then the response status is 400
    And the error detail is "This item does not need booking."

  Scenario: Checklist of an unknown trip
    When I get the checklist of an unknown trip
    Then the response status is 404
