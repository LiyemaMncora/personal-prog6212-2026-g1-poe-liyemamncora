# RaceDay

**Student:** Liyema Mncora

**Student number:** ST10397165

**Qualification:** Bachelor of Computer and Information Sciences in Application Development (BCAD0701)

---

## 1. About the system

RaceDay is a web based event management system for the road running,
walking and cycling community. Many road events are still run with
paper forms and spreadsheets, so RaceDay puts everything in one place.

An Event Organiser can create events, add the categories of the event (like a 10km or
a 21.1km) and capture the results after the race. A Participant can browse the events
that are coming up, enter an event by choosing a category, see the events they entered
and see their own past results.

The project is built in three parts:

| Part   | What it is                                                                   | Where it is     |
| ------ | ---------------------------------------------------------------------------- | --------------- |
| Part 1 | Planning: the ERD, the API endpoint plan and the SQL script                  | /docs         |
| Part 2 | The RESTful API in C# (ASP.NET Core, EF Core, sessions, Swagger, unit tests) | /RaceDay      |
| Part 3 | The MVC web application, Azure Blob Storage and Docker                       | not started yet |


## 2. The roles

| Role            | What they can do                                                                                                                                                      |
| --------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Organiser**   | Create, edit and delete their own events. Add and manage the categories of an event. Capture the results of the participants. See everybody that entered their event. |
| **Participant** | Create an account and log in. Browse the events. Enter an event by choosing a category. See their own enrolments. See their own results and performance history.      |


## 3. What is in this repository

```
/docs                                       
    RaceDay_ERD.png                         
    API_Endpoint_Plan.md                    
    RaceDay_Database.sql                    
    ci-green-build.png
    ST10397165_PROG6212_POE_PART_1.pdf
    ST10397165_PROG6212_POE_PART_2.pdf
/RaceDay                                      
    RaceDay.sln
    /RaceDay.Api                            
        /Controllers (Auth, Roles, Users, Events, Categories, Enrolments, Results)
        /Data                               
        /DTOs                               
        /Helpers                            
        /Models                             
        Program.cs                          
    /RaceDay.Api.Tests                      
/.github/workflows
    validate-docs.yml                       
    build-and-test.yml                      
README.md
```

## 4. The database

The database has six tables: **Roles**, **Users**, **Events**, **Categories**, **Enrolments**`
and **Results**.

- One role is given to many users (`Roles` **1 - 0..\*** `Users`).
- One organiser creates many events (`Users` **1 - 0..\*** `Events`).
- One event offers many categories (`Events` **1 - 0..\*** `Categories`).
- One participant makes many enrolments (`Users` **1 - 0..\*** `Enrolments`).
- One event receives many enrolments (`Events` **1 - 0..\*** `Enrolments`).
- One category is selected in many enrolments (`Categories` **1 - 0..\*** `Enrolments`).
- One enrolment produces at most one result (`Enrolments` **1 - 0..1** `Results`).

Enrolments is the associative entity: one row records the participant, the event and the chosen category, which resolves the many-to-many between participants and events.

The SQL script matches the ERD exactly. There are no differences.

## 5. Setup instructions

**What you need:** SQL Server 2016 or newer and SQL Server Management Studio (SSMS).

1. Download or clone this repository.
2. Open SSMS and connect to your local server (for example localhost\SQLEXPRESS).
3. Click **File > Open > File** and open docs/RaceDay_Database.sql.
4. Click **Execute** (or press F5).
5. The script creates the RaceDayDB database, creates the six tables and adds the
   sample data. The two test queries at the bottom of the script show the events with
   their categories, and the participants with their results.
6. If you run the script again it will not break, because it drops the tables first.

The sample data has 2 organisers, 3 participants, 3 events, 2 categories for each
event, 5 enrolments and 3 results. At the bottom the script has an integrity check query that must return 0 rows.

## 6. How to run the API

**What you need:**
- .NET 8 SDK
- SQL Server 2016 or newer (for example SQL Server Express)
- Visual Studio 2022 (or the `dotnet` command line)

### Step 1 - the connection string
Open `RaceDay/RaceDay.Api/appsettings.json`. The default connection string uses SQL Server Express with
Windows login. Change `Server=` if your instance has a different name (for example `(localdb)\MSSQLLocalDB`).

```json
"ConnectionStrings": {
  "RaceDayDB": "Server=localhost\\SQLEXPRESS;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### Step 2 - create the migration (first time only)

Open a terminal in the `RaceDay/RaceDay.Api` folder:

```
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Step 3 - run it

In Visual Studio set `RaceDay.Api` as the startup project and press **F5**, or in the terminal:

```
cd RaceDay/RaceDay.Api
dotnet run --launch-profile http
```

### Sample accounts

| Role        | Email                         | What they own                                                                 |
| ----------- | ----------------------------- | ----------------------------------------------------------------------------- |
| Organiser   | thabo.nkosi@raceday.co.za     | Durban 10km (already finished, has results) and Pietermaritzburg Half Marathon |
| Organiser   | naledi.mokoena@raceday.co.za  | Cape Winelands Cycle Classic                                                  |
| Participant | sipho.khumalo@gmail.com       | enrolments in the Durban 10km and the Half Marathon                           |
| Participant | chantal.pillay@gmail.com      | enrolments in the Durban 5km and the 25km Cycle                               |
| Participant | lerato.dlamini@gmail.com      | enrolment in the Durban 10km                                                  |

### How to test in Swagger

1. Open `POST /api/auth/login`, click **Try it out**, enter an email and the password above and click **Execute**.
2. The API starts a session and sends a cookie. Swagger UI sends that cookie with every request after that, so
   the protected endpoints now work for that user. (The lock icons are not used, because this is a session cookie and not a token.)
   Only `register`, `login` and `GET /api/roles` can be used without logging in, so log in first even to try `GET /api/events`.
3. To change user, call `POST /api/auth/logout` and log in again with another account.

### Run the tests

```
cd RaceDay
dotnet test
```

---

## 7. The endpoints


| Area           | Method and route                                                                                   | Role                                 |
| -------------- | -------------------------------------------------------------------------------------------------- | ------------------------------------ |
| Authentication | `POST /api/auth/register`, `POST /api/auth/login`                                                  | None                                 |
|                | `POST /api/auth/logout`                                                                            | Any                                  |
| Roles          | `GET /api/roles`                                                                                   | None                                 |
| User profile   | `GET /api/users/me`, `PUT /api/users/me`                                                           | Any                                  |
|                | `GET /api/users/{id}`                                                                              | Organiser                            |
| Events         | `GET /api/events` (filters: `search`, `eventType`, `province`, `upcomingOnly`), `GET /api/events/{id}` | Any                             |
|                | `GET /api/events/my-events`, `POST /api/events`, `PUT /api/events/{id}`, `DELETE /api/events/{id}` | Organiser                            |
| Categories     | `GET /api/events/{eventId}/categories`, `GET /api/categories/{id}`                                 | Any                                  |
|                | `POST /api/events/{eventId}/categories`, `PUT /api/categories/{id}`, `DELETE /api/categories/{id}` | Organiser                            |
| Enrolments     | `POST /api/enrolments`, `GET /api/enrolments/me`, `DELETE /api/enrolments/{id}`                    | Participant                          |
|                | `GET /api/enrolments/{id}`                                                                         | Any (owner or the event's organiser) |
|                | `GET /api/events/{eventId}/enrolments`                                                             | Organiser                            |
| Results        | `POST /api/results`, `PUT /api/results/{id}`, `DELETE /api/results/{id}`                           | Organiser                            |
|                | `GET /api/results/me`                                                                              | Participant                          |
|                | `GET /api/events/{eventId}/results`                                                                | Any                                  |
| *Part 3*       | *`POST /api/users/me/profile-picture`, `POST /api/events/{id}/banner`*                             | *Any / Organiser (not built yet)*    |

---

## 8. Unit tests


| Test class                | What it covers                                                                                                                                |
| ------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| `AuthenticationTests`     | register for both roles, duplicate email, invalid input, password saved as a hash, login success and failure, the session, logout             |
| `RoleTests`               | the public roles list, and that every listed role can be used to register                                                                     |
| `UserProfileTests`        | view and update the profile, organiser looks up a user, a participant is rejected                                                             |
| `EventTests`              | organiser creates, updates and deletes events, distance, other organisers and participants are rejected, viewing needs a login, filters, enrolment counts, validation |
| `CategoryTests`           | create, update and delete categories, ownership, duplicate names, categories with entries cannot be deleted, viewing needs a login            |
| `EnrolmentTests`          | a participant enters a category and the enrolment (including its event) is recorded, race numbers, duplicates, full categories, cancel, organiser start list, roles |
| `ResultTests`             | organiser captures, updates and deletes results, the participant sees only their own results, results in position order, viewing needs a login, validation |
| `PasswordHasherTests`     | plain unit tests for the hashing                                                                                                              |

There are **171 test cases**. Both the success and the failure scenarios are tested (401 not logged in, 403 wrong role or wrong owner,
404, 409 and 400).

---

## 10. CI/CD

There are two workflows in **.github/workflows**:
**validate-docs.yml** runs on every push to **main** and checks that:

- the **/docs** folder exists,
- the ERD image is inside **/docs**,
- **API_Endpoint_Plan.md** is inside **/docs**,
- **RaceDay_Database.sql** is inside **/docs**,
- the SQL script has the six tables,
- the **README** is not empty.

**build-and-test.yml** runs on **every push** and on pull requests to main.

**Screenshot of the green build:**

<img width="1904" height="940" alt="Screenshot 2026-09-22 192127" src="https://github.com/user-attachments/assets/b43d2fc8-5806-4ebe-a35d-4d3e55fd19e6" />

**Screenshot of the green build (Part 2):**



**CI/CD Note**

The validate-docs.yml GitHub Actions workflow runs successfully and produces a green check, confirmed on a personal public mirror of this repository at [github.com/LiyemaMncora/personal-prog6212-2026-g1-poe-liyemamncora](https://github.com/LiyemaMncora/personal-prog6212-2026-g1-poe-liyemamncora). On the official submission repository (under the EMECPE GitHub organisation), the same workflow currently fails to start with the message: _"The job was not started because recent account payments have failed or your spending limit needs to be increased."_ This is a billing/Actions-minutes restriction on the organisation's account, unrelated to the repository content or workflow configuration itself.

## 7. Video presentation

**Part 1:** YouTube: https://youtu.be/kXcVMdF6RlQ 
**Part 2:** YouTube: 
