# Stargate

Solution for the Stargate exercise. Original instructions are in [EXERCISE.md](EXERCISE.md). The first commit is the code as it was given to me, so you can diff against it to see what changed.

## Setup

Needs the .NET 8 SDK, Node 22+, and the EF tools:

```
dotnet tool install --global dotnet-ef --version 8.0.*
```

API - runs on http://localhost:5204 (Swagger at /swagger):

```
cd api
dotnet ef database update
dotnet run
```

UI - runs on http://localhost:4200 and needs the API running:

```
cd ui
npm install
npm start
```

Tests - from the root folder:

```
dotnet test
```

## Fixes

- SQL injection in three of the queries (name was concatenated into the SQL). Switched to Dapper parameters. This also fixes names with an apostrophe.
- `GET /AstronautDuty/{name}` was calling `GetPersonByName`, so it never returned duties.
- Unknown names returned a 200 with `person: null`, and the duties query threw a null reference. Both are 404s now.
- `CreatePersonPreProcessor` wasn't registered with MediatR so the duplicate name check never ran.
- `POST /AstronautDuty` had no try/catch.
- Raw exception messages were being returned to the client.
- `CareerStartDate` was nullable in the migration but not in the model. Added a migration for it.
- Duplicate duty check wasn't filtered by person, and compared a date with a time to a date without one.
- Career end date was the same day as retirement (instead of the day before) when RETIRED was the first duty.
- Added a unique index on `Person.Name` (case insensitive) so the DB enforces rule 1 and not just the pre-processor.

Also added `PUT /Person/{name}` to rename someone, since that's the only thing you can update on a person.

## Assumptions

- New duties have to start after the current one. I reject back-dated duties rather than inserting them into the history.
- No new duties after someone retires.
- RETIRED can't be someone's first duty.
- RETIRED and names aren't case sensitive.
- Dates are whole days, any time portion gets dropped.

## Logging

`LoggingBehavior` is a MediatR pipeline behavior that writes every request to the `ProcessLog` table as Success, Rejected or Error. Bad request bodies get rejected by ASP.NET before they reach MediatR, so `LogValidationErrorsFilter` logs those.

The insert uses Dapper instead of EF. If the handler's SaveChanges failed, the bad changes are still tracked in the context and EF would try to save them again with the log.

## Tests

40 tests, ~75% line coverage. They use an in-memory SQLite db built from the migrations instead of the EF InMemory provider, which can't run the Dapper queries and doesn't enforce unique indexes.

## UI

Single page. People list with search and an add form on the left. Click someone to see their duty history and add a new duty (use RETIRED as the title to retire them).

## Notes / next steps

- .NET 8 goes out of support in November 2026 and a few of the packages have known vulnerabilities. Would upgrade to .NET 10.
- Kept MediatR on 12.x - newer versions need a paid license.
- Two duties posted for the same person at the same time could both pass the "after the current duty" check. Should wrap the check and insert in a transaction.
- Paging on the people list, and some cleanup for old log rows.
- No UI tests yet.
