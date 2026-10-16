# Thando — Part 1: System Planning and Database

## Overview

Thando is a full-stack, web-based event management system built for South Africa's road running, walking, and cycling community. It replaces paper-based registration, spreadsheets, and disconnected communication channels with a single platform.

**Event Organisers** can create and manage events, categories, and participant results.
**Participants** can browse events, enter categories, track personal results, and view their enrolment history.

This is Part 1 of a three-part progressive build. Part 1 covers system planning: the Entity Relationship Diagram, the API Endpoint Plan, and the SQL database script that implements the planned data model.

## Roles

| Role | Permissions |
|---|---|
| **Organiser** | Create, edit, and delete events; manage event categories; capture participant results; view all event enrolments. |
| **Participant** | Create an account; browse events; enter an event by selecting a category; view their own enrolments; track their personal results. |

Role-based access will be enforced at the API level in Part 2 and reflected consistently in the MVC interface in Part 3.

## Repository Structure

```
/docs
  ERD.png                    -- Entity Relationship Diagram (Section A)
  API_Endpoint_Plan.pdf      -- Planned REST API endpoints (Section B)
  Thando_Schema.sql         -- Database creation + seed script (Section C)
.github/workflows/
  ci.yml                     -- CI workflow validating /docs structure
README.md
```

## Database

The schema is implemented in SQL Server (T-SQL) and covers six entities:

- **Users** — holds both Organisers and Participants; `Role` distinguishes them.
- **Venues** — location/route information for an event.
- **Events** — created by an Organiser at a Venue.
- **Categories** — distance/price bands within an Event (e.g. 5km, 10km, Half Marathon).
- **Enrolments** — links a Participant to a Category they've entered.
- **Results** — one result per Enrolment (1:1).

The script drops tables in FK-safe order so it can be re-run cleanly, creates all tables with primary keys, foreign keys, and constraints (`NOT NULL`, `UNIQUE`, `DEFAULT`, `CHECK`), and seeds the database with 2 Organisers, 2 Participants, 3 Events, categories for each event, and sample enrolments/results.

The SQL script matches the ERD exactly — no deliberate deviations.

## Setup Instructions

1. Install SQL Server and SQL Server Management Studio (SSMS).
2. Clone this repository.
3. Open `/docs/Thando_Schema.sql` in SSMS, connected to a clean SQL Server instance.
4. Execute the script. It will create `ThandoDB`, all tables, constraints, and seed data.
5. Confirm the success message: `Thando schema created and seeded successfully.`
6. Verify seed data with, for example:
   ```sql
   USE ThandoDB;
   SELECT * FROM dbo.Events;
   SELECT * FROM dbo.Enrolments;
   ```

## CI/CD

A GitHub Actions workflow (`.github/workflows/ci.yml`) runs on every push and validates that the `/docs` folder exists and contains the required planning files (`ERD.png`, `API_Endpoint_Plan.pdf`, `Thando_Schema.sql`).

**Green build screenshot:**

`[ screenshot placeholder — add your own after a successful Actions run ]`

## Video Walkthrough

**YouTube (unlisted):** attached 

The video walks through the planning documents, the ERD decisions, the endpoint plan choices, and runs the SQL script live in SSMS.
