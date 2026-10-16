# YouTube walkthrough outline (unlisted, ~8-10 min)

The brief requires you to record this yourself in your own voice (no AI voiceover). Suggested flow:

1. **Intro (30s)** — RaceDay, Part 2, what the API does.
2. **Run the API (1 min)** — start it, open Swagger UI, show every endpoint listed with descriptions.
3. **Code structure (2 min)** — Controllers, Models, DbContext (mirrors ERD), Dtos, TokenService, Program.cs.
4. **Authentication (2 min)** — register an Organiser and a Participant in Swagger, log in, paste the token into
   *Authorize*, show `GET /api/users/me`. Explain JWT carries id + role and passwords are hashed (show `Users` table).
5. **Role-based access (1.5 min)** — as Participant try `POST /api/events` (403); as Organiser succeed.
6. **Endpoints demo (2 min)** — create event -> add category -> enrol as Participant -> capture result -> view leaderboard.
7. **Unit tests (1.5 min)** — show test classes, run `dotnet test` (all green), point out success + failure cases.
8. **CI/CD (30s)** — GitHub Actions run, green build.
