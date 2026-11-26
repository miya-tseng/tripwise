Feature: Activity details
  As a traveler
  I want to give each activity a category, booking status, notes and a link
  So that I can see what kind of plan it is and whether it is booked

  Background:
    Given a trip "Tokyo" from 2021-05-01 to 2021-05-03 exists

  Scenario: Add an activity with details
    When I add an activity "Shibuya Sky" on 2021-05-01 with details:
      | category    | bookingStatus | notes             | confirmationCode | link                                         |
      | Sightseeing | Booked        | Sunset at 16:30   | SKY-778          | https://www.shibuya-scramble-square.com/sky/ |
    Then the response status is 201
    And the activity "Shibuya Sky" in the schedule has details:
      | category    | bookingStatus | notes           | confirmationCode | link                                         |
      | Sightseeing | Booked        | Sunset at 16:30 | SKY-778          | https://www.shibuya-scramble-square.com/sky/ |

  Scenario: Activities without details use defaults
    When I add an activity "Free time" on 2021-05-01 from 15:00 to 16:00
    Then the activity "Free time" in the schedule has details:
      | category | bookingStatus | notes | confirmationCode | link |
      | Other    | NotNeeded     |       |                  |      |

  Scenario: Update the booking status
    When I add an activity "teamLab" on 2021-05-01 with details:
      | category   | bookingStatus | notes | confirmationCode | link |
      | Experience | ToBook        |       |                  |      |
    And I mark the activity "teamLab" as "Paid" with confirmation "TL-42"
    Then the response status is 200
    And the activity "teamLab" in the schedule has details:
      | category   | bookingStatus | notes | confirmationCode | link |
      | Experience | Paid          |       | TL-42            |      |

  Scenario: Moving an activity keeps its details
    When I add an activity "Sushi" on 2021-05-01 with details:
      | category | bookingStatus | notes       | confirmationCode | link |
      | Food     | Booked        | Window seat | S-1              |      |
    And I move the added activity "Sushi" to 2021-05-02 at 12:00
    Then the activity "Sushi" in the schedule has details:
      | category | bookingStatus | notes       | confirmationCode | link |
      | Food     | Booked        | Window seat | S-1              |      |

  Scenario: Link must use https
    When I add an activity "Shibuya Sky" on 2021-05-01 with details:
      | category    | bookingStatus | notes | confirmationCode | link                   |
      | Sightseeing | NotNeeded     |       |                  | http://example.com/sky |
    Then the response status is 400
    And the error detail is "Link must be an https URL."

  Scenario: Unknown category
    When I add an activity "Shibuya Sky" on 2021-05-01 with category "Nightlife"
    Then the response status is 400

  Scenario: Notes are too long
    When I add an activity "Shibuya Sky" on 2021-05-01 with 2001 characters of notes
    Then the response status is 400
