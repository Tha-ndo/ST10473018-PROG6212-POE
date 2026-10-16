# Suggested commit sequence (20+ meaningful commits)

The brief requires at least 20 meaningful commits from **your own** GitHub account. Build the repo up in stages so the
history reflects real progress. Use a `part2-api` branch (or a `Part2` subfolder) as instructed. Suggested order:

1.  Add solution and empty Web API project (`RaceDay.sln`, `RaceDay.Api.csproj`)
2.  Add .gitignore
3.  Add EF Core entities matching the Part 1 ERD
4.  Add RaceDayDbContext with keys, constraints and relationships
5.  Add appsettings connection string and JWT settings
6.  Add DbSeeder with Part 1 reference data
7.  Add DTOs and validation rules
8.  Add TokenService and JWT authentication setup in Program.cs
9.  Add AuthController (register + login with hashed passwords)
10. Add UsersController (profile endpoints)
11. Add VenuesController
12. Add EventsController (browse + Organiser CRUD)
13. Add CategoriesController
14. Add EnrolmentsController
15. Add ResultsController
16. Add Swagger with JWT support and XML documentation
17. Generate the InitialCreate EF Core migration
18. Add test project and WebApplicationFactory setup
19. Add authentication tests
20. Add event management tests
21. Add role-based access tests
22. Add enrolment tests
23. Add result tests
24. Add GitHub Actions CI workflow
25. Add README (setup, roles, CI screenshot, video link)

Tip: `git add <specific files>` per step and commit with a clear message, rather than one big commit at the end.
