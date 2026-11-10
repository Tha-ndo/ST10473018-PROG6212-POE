# RaceDay

RaceDay is an event platform for South African road running, walking and cycling events.

| Part | Project | What it does |
|------|---------|--------------|
| 2 | `src/RaceDay.Api` | ASP.NET Core 8 Web API with EF Core, SQL Server, JWT auth and Swagger |
| 2 | `tests/RaceDay.Api.Tests` | xUnit integration tests (in-memory database, fake blob storage) |
| **3 `src/RaceDay.Web`** | **ASP.NET Core 8 MVC front end. It talks to the API for every read and write.** |


---

## Architecture

```
 Browser ──HTTP──► RaceDay.Web (MVC, Docker)  ──HTTP + JWT──►  RaceDay.Api  ──EF Core──► SQL Server
                    • Razor views + Bootstrap                    • business rules           
                    • session holds the JWT                      • validation & ownership   ──SDK──► Azure Blob Storage
                    • NO database, NO business rules             • image uploads              (banners, profile pictures)
```

* **The MVC app never touches the database.** It has no EF Core reference and no connection string. All data goes through `Services/RaceDayApiClient.cs`, a typed `HttpClient`.
* **The API holds the business rules:** event dates, category capacity, ownership, result validation and image type and size limits. MVC shows the API's validation messages next to the matching form fields.
* **The API is the only part that talks to Azure.** The MVC app uploads files to the API as `multipart/form-data`, and the API stores them in Azure Blob Storage.

### Session-based authentication (MVC)

1. On **login/register**, MVC calls `POST /api/auth/login`, then `GET /api/users/me`.
2. It stores the JWT, user id, **role**, name and photo URL in the **server-side ASP.NET Core session**. The browser only holds an HttpOnly session cookie.
3. Every API call adds `Authorization: Bearer <token>` from the session.
4. `[RequireLogin]` / `[RequireLogin("Organiser")]` (in `Infrastructure/RequireLoginAttribute.cs`) protect pages:
   * anonymous users go to **Log in** and come back to the page they asked for afterwards;
   * logged-in users with the wrong role go to **Access denied**;
   * protected pages are sent with `Cache-Control: no-store`, so the browser's Back button can't show them after logout.
5. When the token expires, or the API answers 401, the session is cleared and the user goes back to **Log in** with a message.
6. **Log out** clears the session and returns to the login page.

---

## Features

### Organiser ("Organiser HQ" — yellow badge)
| Page | What it does |
|------|--------------|
| **Dashboard** | Totals (events, confirmed enrolments, upcoming, next event date), then one card per event with enrolments against capacity, categories, results status and quick links. |
| **New / Edit event** | Name, description, date, event type, location (venue) and the first distance category, plus **banner image upload** to Azure. You can also delete an event or remove its banner. |
| **Categories** | Add, edit inline and remove distance or age categories, with an "enrolled" bar for each category. |
| **Enrolments** | Every participant for an event, with race number, category and colour-coded status. Search and filter by category or status. |
| **Results** | Capture and publish finish time, position and status (Finished / DNF / DNS) for each confirmed participant, and correct them later. |
| **Add venue** | Create a new location to choose when creating events. |

### Participant ("Athlete" — green badge)
| Page | What it does |
|------|--------------|
| **Home** | All upcoming events as a list, with a filter panel (search, type, province, date range) applied **by the API**. Events you've entered show an "Enrolled" ribbon. |
| **Event detail** | Banner (from Azure), description, venue with a map link, categories with fees, a one-click **Enrol** button (saved immediately) and the leaderboard. |
| **My enrolments** | Upcoming, past and cancelled enrolments with race numbers and status badges. Upcoming ones can be cancelled. |
| **My results** | Race history (event, date, category, finish time, **"47th out of 312 finishers"**) plus personal stats. |
| **Profile** (both roles) | Update name and phone, and **upload a profile picture** to Azure. |

### UI
* "Trail" RaceDay theme on **Bootstrap 5.3**: Space Grotesk headings, DM Sans body text, forest green / track yellow palette. App-shell layout with a **left sidebar** (slide-out menu on phones), a filter panel beside a list of event rows, and a card grid on the Organiser dashboard. Everything comes from one stylesheet (`wwwroot/css/site.css`), so all pages share the same fonts, colours, spacing and components.
* Bootstrap, the icons and the fonts are **self-hosted** in `wwwroot/lib`, so the container works without a CDN.
* Status colours come from one helper (`StatusStyles`): **green** = Confirmed/Finished/Published, **amber** = DNS/Awaiting result, **red** = Cancelled/DNF.
* Errors are handled without crashing: API validation errors show inline, there are friendly 404/500 pages, and a "service unavailable" page appears if the API is down. Users never see raw exceptions or JSON.
* Responsive, keyboard-accessible (skip link, focus rings, labelled controls).

---

## Run locally

### Prerequisites
* .NET 8 SDK
* SQL Server LocalDB (Visual Studio) or another SQL Server (change `ConnectionStrings:DefaultConnection`)
* An **Azure Storage account**, or the **Azurite** emulator for local development
* Docker Desktop (for the container)

### 1. Configure Azure Blob Storage (API)

**Option A — real Azure (use this for the demo):**
1. Azure Portal → *Storage accounts* → **Create** (Standard, LRS).
2. Storage account → *Settings → Configuration* → set **Allow Blob anonymous access** to **Enabled** and click **Save**. This lets browsers display the images.
3. *Security + networking → Access keys* → copy **Connection string**.
4. Store it as a user secret so the key never ends up in Git:
   ```bash
   cd src/RaceDay.Api
   dotnet user-secrets init
   dotnet user-secrets set "AzureStorage:ConnectionString" "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
   ```
   The API creates the `raceday-images` container (with public read access for blobs) the first time it uploads.

**Option B — Azurite emulator:** the default setting `UseDevelopmentStorage=true` already points at it.
```bash
docker run -d -p 10000:10000 --name azurite mcr.microsoft.com/azure-storage/azurite azurite-blob --blobHost 0.0.0.0
```

### 2. Start the API (Part 2)
```bash
dotnet run --project src/RaceDay.Api --launch-profile http
# Swagger: http://localhost:5080/swagger
```
On startup the API applies migrations (including Part 3's `AddImageUrls`) and seeds demo data.

### 3. Start the MVC app (Part 3)
```bash
dotnet run --project src/RaceDay.Web --launch-profile http
# http://localhost:5201
```
The API address comes from `Api:BaseUrl` in `src/RaceDay.Web/appsettings.json` (default `http://localhost:5080/`).

### Demo accounts (seeded, password `Password123!`)
| Role | Email |
|------|-------|
| Organiser | `thandiwe@comrades.org.za` |
| Organiser | `pieter@capetowncycle.co.za` |
| Participant | `sipho.nkosi@example.com` |
| Participant | `amanda.botha@example.com` |

---

## Run with Docker

The `Dockerfile` in the repository root uses a **multi-stage build**: the .NET 8 **SDK** image restores and publishes in Release mode, then only the published output is copied into the small **ASP.NET runtime** image. The app runs as the **non-root** user on port **8080**. Restore runs in its own layer, so it is cached until the project file changes, and `.dockerignore` keeps `bin/`, `obj/`, tests and secrets out of the build context.

**1. Build the image** (from the repository root):
```bash
docker build -t raceday-web .
```

**2. Run it — single command** (with the API running on your machine, step 2 above):
```bash
docker run --rm -p 8080:8080 -e Api__BaseUrl=http://host.docker.internal:5080/ --add-host=host.docker.internal:host-gateway --name raceday-web raceday-web
```
Open **http://localhost:8080**. The container uses the same pages, session login and API calls as `dotnet run`. Only the port is different.

> **Troubleshooting:** if the container shows *"Taking a water break"* (it can't reach the API), make sure the API is running on port 5080. If it still fails, start the API so it listens on all interfaces: `dotnet run --project src/RaceDay.Api --urls http://0.0.0.0:5080`.
> Health check: `http://localhost:8080/health`.

---

## API additions for Part 3

| Method | Endpoint | Role | Purpose |
|--------|----------|------|---------|
| GET | `/api/events?type=&province=&search=&from=&to=` | Public | Upcoming events with **filters**. Results now include `bannerImageUrl` and `minDistanceKm`/`maxDistanceKm`. |
| GET | `/api/events/mine` | Organiser | The Organiser's own events (past and future) with enrolment, capacity and results totals, for the dashboard |
| POST | `/api/events/{id}/banner` | Organiser (owner) | Upload or replace the banner in **Azure Blob Storage** (multipart field `file`; JPG/PNG/WEBP/GIF, ≤ 5 MB) |
| DELETE | `/api/events/{id}/banner` | Organiser (owner) | Remove the banner (the blob is deleted too) |
| POST | `/api/users/me/photo` | Any logged-in user | Upload or replace the profile picture in **Azure Blob Storage** |

Other changes:
* `EventDetailDto.bannerImageUrl`, `UserDto.profilePictureUrl`.
* `ResultDto` now includes `eventDate`, `distanceKm` and `totalFinishers` (finishers in the same category), used for "47th out of 312".
* New EF Core migration `AddImageUrls` (two nullable `nvarchar(500)` columns).
* **Bug fix:** `EnrolmentsController` was missing `[ApiController]` and `[Route("api")]`, so its endpoints were served without the `/api` prefix.
* Azure code: `Infrastructure/Storage/IFileStorageService.cs` and `AzureBlobStorageService.cs` (official `Azure.Storage.Blobs` SDK). Upload rules live in `ImageUploadRules.cs`.
* HTTPS redirection is skipped in Development. Otherwise `HttpClient` drops the `Authorization` header when it follows the redirect.

---

## Tests & CI

```bash
dotnet test
```
`tests/RaceDay.Api.Tests/Part3ApiTests.cs` covers the new endpoints: own events and totals, filters, banner upload (success, wrong file type, another organiser's event), profile picture, and total finishers. `RaceDayFactory` swaps Azure for an in-memory `FakeFileStorage`, so the tests need no cloud account.

GitHub Actions (`.github/workflows/ci.yml`) runs on every push:
1. restore → build the whole solution → **build the MVC project** → run the tests;
2. **build the Docker image** and smoke-test the container's `/health` endpoint.

---

## Project structure (Part 3)

```
src/RaceDay.Web/
├── Controllers/        Account, Home, Events, Participant, Organiser, Profile
├── Infrastructure/     RequireLoginAttribute (session auth), ApiExceptionFilter, helpers (status colours, formatting)
├── Models/Api/         Copies of the API's JSON contracts (no reference to the API project)
├── Models/ViewModels/  Form & page models
├── Services/           RaceDayApiClient (typed HttpClient), UserSession (session state), ApiResult
├── Views/              Razor views; Shared/_Layout has role-specific navigation
└── wwwroot/            site.css (theme), site.js, self-hosted Bootstrap 5.3, icons and fonts
Dockerfile, .dockerignore   (repository root)
```
