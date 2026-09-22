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
    ST10397165_PROG6212_POE_PART.pdf
/.github/workflows
    validate-docs.yml
README.md
```

## 4. The database

The database has six tables: **Roles**, **Users**, **Events**, **Categories**, **Enrolments**`
and **Results**.

- One role is given to many users.
- One organiser creates many events.
- One event contains many categories.
- One category is entered by many participants and one participant can enter many
  categories.
- One enrolment has one result.

The SQL script matches the ERD exactly. There are no differences.

## 5. Setup instructions

**What you need:** SQL Server 2016 or newer and SQL Server Management Studio (SSMS).

1. Download or clone this repository.
2. Open SSMS and connect to your local server (for example localhost\SQLEXPRESS).
3. Click **File > Open > File** and open docs/RaceDay_Database.sql.
4. Click **Execute** (or press F5).
5. The script creates theRaceDayDB database, creates the six tables and adds the
   sample data. The two test queries at the bottom of the script show the events with
   their categories, and the participants with their results.
6. If you run the script again it will not break, because it'd drops the tables first.

The sample data has 2 organisers, 3 participants, 3 events, 2 categories for each
event, 5 enrolments and 3 results.

## 6. CI/CD

The workflow file is **.github/workflows/validate-docs.yml**. It runs on every push to
**main** and checks that:

- the **/docs** folder exists,
- the ERD image is inside **/docs**,
- **API_Endpoint_Plan.md** is inside **/docs**,
- **RaceDay_Database.sql** is inside **/docs**,
- the **README** is not empty.

**Screenshot of the green build:**

<img width="1904" height="940" alt="Screenshot 2026-09-22 192127" src="https://github.com/user-attachments/assets/b43d2fc8-5806-4ebe-a35d-4d3e55fd19e6" />

**CI/CD Note**

The validate-docs.yml GitHub Actions workflow runs successfully and produces a green check, confirmed on a personal public mirror of this repository at [github.com/LiyemaMncora/personal-prog6212-2026-g1-poe-liyemamncora](https://github.com/LiyemaMncora/personal-prog6212-2026-g1-poe-liyemamncora). On the official submission repository (under the EMECPE GitHub organisation), the same workflow currently fails to start with the message: _"The job was not started because recent account payments have failed or your spending limit needs to be increased."_ This is a billing/Actions-minutes restriction on the organisation's account, unrelated to the repository content or workflow configuration itself.
