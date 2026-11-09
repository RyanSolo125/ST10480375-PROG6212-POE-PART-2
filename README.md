# RaceDay – Part 3

**RaceDay** is a platform for organising and entering running, walking and cycling events.
Part 3 brings everything together:

- **RaceDay.Api** – the Part 2 ASP.NET Core Web API (EF Core + SQL Server, session authentication), now with **Azure Blob Storage** image uploads
- **RaceDay.Web** – a new **ASP.NET Core MVC** front end that gets **all** of its data from the API
- **RaceDay.Tests** – xUnit unit tests for the API
- **Docker** – the MVC app runs in a container
- **GitHub Actions** – builds the API and MVC projects, runs the tests and builds the Docker image on every push

**Student:** Ryan Solomon – ST10480375 – PROG6212

---

## Architecture

```
 Browser ──► RaceDay.Web (MVC, Docker :8080) ──HTTP──► RaceDay.Api (:5180) ──► SQL Server (EF Core)
                     no database, no Azure                     │
                                                               └──► Azure Blob Storage (images)
```

- The MVC project has **no database access and no business rules**. Every page calls the API through one class, `Services/RaceDayApiClient.cs`.
- At login the API returns its session cookie. The MVC app keeps it in the MVC session and sends it with every API call, so the API knows who the user is and what role they have.
- Images are uploaded **to the API**, which stores them in Azure Blob Storage. They are shown through `GET /api/files/{name}`, so the MVC app never talks to Azure.

## The two roles

| Role | What they can do in the MVC app |
|---|---|
| **Organiser** | **Dashboard** with total events, total enrolments, upcoming events and enrolments per event. **Create, edit and delete events** (name, description, date, location, distance, type) and **upload a banner image**. **Manage categories** (age or distance) for each event. **View enrolments** per event with participant, category and colour-coded status, and confirm or cancel them. **Capture results** (finish time, position, Finished/DNF/DNS) after the event. |
| **Participant** | **Home page** of upcoming events with search and type filters. **Event page** with full details, banner and categories. **Enrol** by choosing a category (recorded straight away as *Pending* until the organiser confirms). **My Enrolments** with status. **My Results**: event, date, category, finish time and position, e.g. *47th out of 312 finishers*. **Profile** page to update details and upload a profile picture. |

Visitors who are not logged in can browse events. Every other page sends them to the login page. Logging out ends the session in both the MVC app and the API, then redirects to the home page.

Enrolment statuses are colour coded: **green = Confirmed**, **amber = Pending**, **red = Cancelled**.

---

## Setup and run (Visual Studio)

### 1. Database
1. Open `Part2-RaceDayApi/RaceDay.sln` in Visual Studio 2022 (.NET 8 SDK).
2. In `RaceDay.Api/appsettings.json`, set the `RaceDayDB` connection string to your SQL Server.
3. In the Package Manager Console run:
   ```
   Update-Database -Project RaceDay.Api -StartupProject RaceDay.Api
   ```

### 2. Azure Blob Storage
1. In the [Azure portal](https://portal.azure.com) create a **Storage account** (Standard, LRS).
2. Open the storage account, go to **Security + networking → Access keys**, and copy a **Connection string**.
3. In Visual Studio, right-click **RaceDay.Api → Manage User Secrets** and paste:
   ```json
   {
     "AzureStorage": {
       "ConnectionString": "PASTE-YOUR-CONNECTION-STRING-HERE"
     }
   }
   ```
   User Secrets keep the key out of GitHub. The private container `raceday-images` is created automatically on the first upload.

### 3. Run the API and the MVC app together
1. Right-click the solution → **Configure Startup Projects…** → **Multiple startup projects**, and set **RaceDay.Api** and **RaceDay.Web** to **Start**.
2. Press **F5**. Swagger opens for the API (`https://localhost:7180/swagger`) and the website opens at `https://localhost:7200`.

**Sample logins** (password `Password123!`):

| Email | Role |
|---|---|
| thandiwe.nkosi@raceday.co.za | Organiser |
| pieter.vanwyk@raceday.co.za | Organiser |
| amahle.dlamini@gmail.com | Participant |
| johan.botha@gmail.com | Participant |

---

## Run the MVC app in Docker

The API must be running first: start it from Visual Studio, where it listens on `http://0.0.0.0:5180`.
Then, from the `Part2-RaceDayApi` folder:

```bash
docker build -t raceday-web ./RaceDay.Web
```

**Run command (starts the app with a single command):**

```bash
docker run -d -p 8080:8080 --name raceday-web raceday-web
```

Open **http://localhost:8080**. The container reaches the API at `http://host.docker.internal:5180/`, which is set in the Dockerfile.
To use a different API address: `docker run -d -p 8080:8080 -e ApiBaseUrl=http://my-api:5180/ --name raceday-web raceday-web`.
On Linux, add `--add-host=host.docker.internal:host-gateway`.

**Dockerfile best practices used:** multi-stage build (SDK image to build, small ASP.NET runtime image to run), restore layer cached separately from the source, `.dockerignore` to keep `bin/` and `obj/` out, runs as the non-root `app` user, port and environment set with `ENV`/`EXPOSE`.

Stop and remove the container: `docker rm -f raceday-web`

---

## API endpoints added for Part 3

| Method | Route | Role |
|---|---|---|
| GET | /api/events?type=&search=&upcoming=true | None |
| GET | /api/events/mine | Organiser |
| POST | /api/events/{id}/banner (multipart) | Organiser (owner) |
| POST | /api/users/me/profile-picture (multipart) | Any |
| GET | /api/files/{fileName} | None |
| PUT | /api/enrolments/{id}/status | Organiser (owner) |

The results and history endpoints now also return `totalFinishers`, used for "47th out of 312". All endpoints are documented in Swagger.

## MVC project structure (`RaceDay.Web`)

```
Controllers/   Home, Account, Events, Enrolments, Results, Profile, Images (participant + shared)
               Dashboard, ManageEvents, Categories, EventEnrolments, EventResults (organiser)
               BaseController / OrganiserBaseController (shared helpers)
Helpers/       RequireLoginAttribute (blocks pages by login/role), SessionKeys, Ui (badges, "47th", avatars)
Models/Api/    Classes matching the API's JSON
Models/ViewModels/  Form models with validation (DataAnnotations)
Services/      RaceDayApiClient – the only place that talks to the API
Views/         Razor pages using Bootstrap 5 and the RaceDay theme (wwwroot/css/site.css)
Dockerfile     Container build for the MVC app
```

Invalid input is caught by form validation and by the API, then shown as friendly messages. Unexpected errors and missing pages show a branded error page instead of raw errors.

## Tests and CI/CD
- Run the tests in Visual Studio with **Test → Run All Tests**, or `dotnet test` in `Part2-RaceDayApi`.
- The tests use an in-memory database, a fake session and a fake file storage, so no SQL Server or Azure account is needed.
- `.github/workflows/dotnet.yml` restores, builds the solution (API + MVC + tests), runs the unit tests and builds the MVC Docker image on every push.

<img width="1882" height="786" alt="image" src="https://github.com/user-attachments/assets/00fafe75-3dc5-4fd2-ae5c-add0ada7234e" />


## Video
YouTube (unlisted): https://youtu.be/cYeBTts5A04?si=kQP0KjXOy4ZQ23Gz
