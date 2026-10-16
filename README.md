# Thando (RaceDay) - Part 2: RESTful API

ASP.NET Core 8 Web API with Entity Framework Core (code-first, SQL Server), JWT authentication and role-based access. It implements the endpoint plan from Part 1 (see `docs/Part1`).

## Roles

| Role | What they can do |
|---|---|
| **Organiser** | Create, update and delete their own events and categories; add venues; view enrolments for their events; capture and correct results. |
| **Participant** | Browse events; enrol in a category (bib number allocated); view and cancel their own enrolments; view their own results. |
| **Anyone (no login)** | Browse events, categories, venues and the public results list for an event. |

## Project structure

```
src/RaceDay.Api/
  Controllers/   Auth, Users, Venues, Events, Categories, Enrolments, Results
  Data/          RaceDayDbContext, DbSeeder
  Dtos/          Request/response models with validation rules
  Extensions/    Mappers, claims helpers
  Migrations/    EF Core InitialCreate migration
  Models/        Entities and Constants (roles, statuses)
  Services/      JWT token service
tests/RaceDay.Api.Tests/   xUnit tests (in-memory database)
docs/                      Part 1 documents and planning notes
.github/workflows/ci.yml   Build and test on every push
```

## Setup

1. Install the .NET 8 SDK and Visual Studio 2022 (this includes SQL Server LocalDB).
2. Clone the repository and switch to the `part2-api` branch.
3. Open `RaceDay.sln` in Visual Studio.
4. The connection string in `src/RaceDay.Api/appsettings.json` uses LocalDB (`(localdb)\MSSQLLocalDB`, database `RaceDayApiDB`). Change it if you use a different SQL Server instance.
5. Press F5 (or run `dotnet run --project src/RaceDay.Api`). The database is created from the migration and seeded on first start.
6. Swagger UI opens at `http://localhost:5080/swagger`.

## Using the API

1. `POST /api/auth/register` to create an Organiser or Participant.
2. `POST /api/auth/login` to receive a JWT.
3. Click **Authorize** in Swagger and paste the token. It is then sent with every request.

## Endpoints

| Area | Endpoints |
|---|---|
| Auth | `POST /api/auth/register`, `POST /api/auth/login` |
| Users | `GET /api/users/me`, `PUT /api/users/me`, `GET /api/users/{id}` (Organiser) |
| Venues | `GET /api/venues`, `POST /api/venues` (Organiser) |
| Events | `GET /api/events`, `GET /api/events/{id}`, `POST`, `PUT /{id}`, `DELETE /{id}` (Organiser, owner) |
| Categories | `GET`/`POST /api/events/{eventId}/categories`, `PUT`/`DELETE /api/categories/{id}` (Organiser, owner) |
| Enrolments | `POST /api/categories/{categoryId}/enrolments`, `GET /api/enrolments/my`, `DELETE /api/enrolments/{id}` (Participant); `GET /api/events/{eventId}/enrolments` (Organiser, owner) |
| Results | `POST /api/enrolments/{enrolmentId}/results`, `PUT /api/results/{id}` (Organiser, owner); `GET /api/results/my` (Participant); `GET /api/events/{eventId}/results` (public) |

## Testing

```
dotnet test RaceDay.sln
```

The tests use xUnit and `WebApplicationFactory` with an in-memory database, so they do not need SQL Server. They cover registration and login, event and category management, role-based access, enrolments and results.

## CI/CD

GitHub Actions (`.github/workflows/ci.yml`) restores, builds and runs all tests on every push.
