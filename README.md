# RaceDay API – Part 2

An ASP.NET Core Web API (.NET 8) for the **RaceDay** system. It uses Entity Framework Core (Code-First) with SQL Server, session-based authentication with two roles, Swagger documentation, xUnit unit tests and a GitHub Actions CI pipeline.

## Roles

| Role | What they can do |
|---|---|
| **Organiser** | Create, update and delete their own events. Add, update and remove age/distance categories on their events. View everyone enrolled in their events. Capture and correct finish times and positions after an event. |
| **Participant** | Enter an event by choosing a category. View and cancel their own enrolments. View their own results and full enrolment/result history. |

Both roles can register, log in and out, and view and update their own profile. Anyone, logged in or not, can browse events, categories and event leaderboards.

## Tech stack
- ASP.NET Core Web API (.NET 8, C#)
- Entity Framework Core 8 – Code-First with migrations
- SQL Server / SQL Server Express
- Session management (`AddSession`) for authentication
- BCrypt password hashing
- Swagger (Swashbuckle) with XML comments
- xUnit + EF Core InMemory for unit tests
- GitHub Actions for CI

## Project structure
```
Part2-RaceDayApi/
  RaceDay.sln
  RaceDay.Api/
    Controllers/   BaseApiController (session + role checks), Auth, Users, Events, Categories, Enrolments, Results
    Data/          RaceDayDbContext (tables + relationships), DbSeeder (sample data)
    Dtos/          Request/response objects (keeps password hashes out of responses)
    Helpers/       SessionKeys, Roles, EventTypes
    Models/        User, Event, Category, Enrolment, Result, Payment
    Program.cs     EF Core, session, Swagger setup
  RaceDay.Tests/   Unit tests (FakeSession + in-memory database)
.github/workflows/dotnet.yml   CI: build + test on every push
```

## Setup and run
1. Install **Visual Studio 2022** (ASP.NET and web development workload), the **.NET 8 SDK** and **SQL Server Express**.
2. Clone the repo and open `Part2-RaceDayApi/RaceDay.sln`.
3. In `RaceDay.Api/appsettings.json`, set the `RaceDayDB` connection string to your SQL Server, e.g.
   `Server=YOUR-PC\\SQLEXPRESS;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True`
4. Open **Tools → NuGet Package Manager → Package Manager Console** (Default project: `RaceDay.Api`) and run:
   ```
   Add-Migration InitialCreate
   Update-Database
   ```
   This creates the `RaceDayDB` database and all its tables from the model classes.
5. Press **F5**. Swagger opens at `https://localhost:7180/swagger`. Sample data is added on the first run.

**Sample logins** (password for all: `Password123!`)

| Email | Role |
|---|---|
| thandiwe.nkosi@raceday.co.za | Organiser |
| pieter.vanwyk@raceday.co.za | Organiser |
| amahle.dlamini@gmail.com | Participant |
| johan.botha@gmail.com | Participant |

## Authentication
- `POST /api/auth/register` – choose role `Organiser` or `Participant`. Any other role is rejected with 400. Passwords are hashed with **BCrypt** and never stored in their original form.
- `POST /api/auth/login` – checks the password hash, then stores the user's **UserId and Role in the server session**. The response returns `{ token, userId, fullName, role }`, where `token` is the session ID. The browser also receives it as the `RaceDay.Session` cookie and sends it back on every request.
- Every protected endpoint checks the session through `BaseApiController`:
  - not logged in → **401 Unauthorized**
  - wrong role (e.g. a Participant creating an event) → **403 Forbidden**
  - not the owner (e.g. an Organiser editing someone else's event) → **403 Forbidden**
- `POST /api/auth/logout` clears the session. Sessions expire after 30 minutes without activity.

## Endpoints

| Method | Route | Role |
|---|---|---|
| POST | /api/auth/register | None |
| POST | /api/auth/login | None |
| POST | /api/auth/logout | Any |
| GET | /api/users/me | Any |
| PUT | /api/users/me | Any |
| GET | /api/users/me/history | Participant |
| GET | /api/events | None |
| GET | /api/events/{id} | None |
| POST | /api/events | Organiser |
| PUT | /api/events/{id} | Organiser (owner) |
| DELETE | /api/events/{id} | Organiser (owner) |
| GET | /api/events/{eventId}/categories | None |
| POST | /api/events/{eventId}/categories | Organiser (owner) |
| PUT | /api/categories/{id} | Organiser (owner) |
| DELETE | /api/categories/{id} | Organiser (owner) |
| POST | /api/enrolments | Participant |
| GET | /api/enrolments/me | Participant |
| GET | /api/events/{eventId}/enrolments | Organiser (owner) |
| DELETE | /api/enrolments/{id} | Participant (own) |
| POST | /api/results | Organiser (owner) |
| PUT | /api/results/{id} | Organiser (owner) |
| GET | /api/results/me | Participant |
| GET | /api/events/{eventId}/results | None |

All endpoints, with their descriptions, request bodies and responses, can be viewed and tested in Swagger.

## Changes from the Part 1 plan
- **`DistanceKm` added to Events.** The brief requires each event to capture a distance.
- **EventType is restricted to `Run`, `Walk` or `Cycle`.** These are the types named in the brief.
- **Login returns the session ID as `token`.** The brief requires session management, so no JWT is used.
- **Added `POST /api/auth/logout`** so a session can be ended.

## Running the tests
- Visual Studio: **Test → Run All Tests**
- Command line, from the `Part2-RaceDayApi` folder: `dotnet test`

The tests call the controllers directly with an **in-memory database** and a **fake session**, so they need no SQL Server. They cover:
- Registration and login: success, password hashing, invalid role, duplicate email, wrong password, logout
- Event management: an Organiser can create, update and delete; a Participant gets 403; a logged-out user gets 401; another organiser can't edit someone else's event
- Role checks: wrong roles are rejected on events, categories, enrolments, results and history
- Enrolments: a Participant can enrol and the participant/event/category link is recorded; duplicates, full categories, past events and unknown categories are rejected
- Results and profile: capture, invalid time, own results only, profile update

## CI/CD
GitHub Actions (`.github/workflows/dotnet.yml`) restores, builds and runs all unit tests on every push.

![Successful build](docs/ci-build.png)

## Video
YouTube (unlisted): **ADD YOUR LINK HERE**
