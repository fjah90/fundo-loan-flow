# Fundo Loan Flow

**Video:** _link added after recording_

A loan application form posts to a .NET API that runs a rule engine and stores the result in PostgreSQL with a transactional outbox.
A background worker then syncs approved applications to a mock external service over HTTP.

## Stack

.NET 10, EF Core 10 + Npgsql, PostgreSQL 18, xUnit v3 + Testcontainers, Next.js 16 + React 19 + Tailwind CSS 4, Node 26, Docker Compose.

## Repository layout

```
fundo-loan-flow/
├── backend/
│   ├── src/LoanFlow.Domain/          Entities, value objects, rule engine. No dependencies.
│   ├── src/LoanFlow.Application/     Use case, event handler, ports. Depends on Domain.
│   ├── src/LoanFlow.Infrastructure/  EF Core + Npgsql, outbox, HTTP client, background worker.
│   ├── src/LoanFlow.Api/             Thin controller, composition root (Program.cs), configuration.
│   └── tests/LoanFlow.Tests/         Unit + integration + endpoint tests (xUnit v3, Testcontainers PostgreSQL).
├── web/                              Next.js App Router: form, approved page, denied page.
├── external-mock/                    ~50-line Node HTTP server that stores what it receives.
├── ARCHITECTURE.md                   Layers, rule engine, transaction, outbox, trade-offs.
├── global.json                       Pins the .NET SDK and selects the Microsoft.Testing.Platform test runner.
└── compose.yaml                      db (PostgreSQL 18) + api + web + external-mock.
```

## Requirements

| To do this | You need |
|---|---|
| Run everything | Docker with Compose v2 |
| Run the tests | .NET 10 SDK (`10.0.403`, pinned in `global.json` with `rollForward: latestPatch`) and a running Docker daemon: Testcontainers starts PostgreSQL 18 |
| Run without Docker | .NET 10 SDK, Node 26, pnpm 12.15.0, PostgreSQL 18 |

## Run with Docker

From the repository root:

```bash
docker compose up --build
```

The first build takes a few minutes. Run `docker compose ps` in another terminal: when `db` and `external-mock` show `healthy` and `api` and `web` show `running`, open the web app.

| Service | URL on your machine | Notes |
|---|---|---|
| web | http://localhost:3080 | Loan application form |
| api | http://localhost:5080 | `POST /api/loan-applications` |
| external-mock | http://localhost:4080 | `GET /customers` lists what the API synced |
| db | `localhost:5442` | PostgreSQL 18, database, user and password all `loanflow` |

Stop the stack with `docker compose down`. Data survives in the `loanflow-pgdata` volume. To reset the database, run `docker compose down -v`, which deletes that volume.

## Run without Docker

Use three terminals from the repository root. You also need a PostgreSQL 18 database (see below).

1. Mock service (port 4000):

   ```bash
   node external-mock/server.js
   ```

2. API (port 5080):

   ```bash
   dotnet run --project backend/src/LoanFlow.Api --urls http://localhost:5080
   ```

3. Web (port 3000):

   ```bash
   pnpm --dir web install
   pnpm --dir web dev
   ```

   Node 26 does not bundle corepack, so install pnpm with `npm install -g pnpm@12.15.0` if you do not have it. The web server calls the API at `API_URL`, which defaults to `http://localhost:5080`. The API calls the mock at `ExternalService:BaseUrl`, which defaults to `http://localhost:4000/`.

Open http://localhost:3000.

### Database

The API connects with the `ConnectionStrings:LoanFlow` value from `appsettings.json`:

```
Host=localhost;Port=5432;Database=loanflow;Username=loanflow;Password=loanflow
```

Any PostgreSQL 18 that accepts that string works. The API creates the schema on startup (`EnsureCreated`), so there is nothing to migrate.

To reuse only the compose database, start it and point the API at its host port 5442:

```bash
docker compose up -d db
export ConnectionStrings__LoanFlow="Host=localhost;Port=5442;Database=loanflow;Username=loanflow;Password=loanflow"
dotnet run --project backend/src/LoanFlow.Api --urls http://localhost:5080
```

PowerShell:

```powershell
docker compose up -d db
$env:ConnectionStrings__LoanFlow = "Host=localhost;Port=5442;Database=loanflow;Username=loanflow;Password=loanflow"
dotnet run --project backend/src/LoanFlow.Api --urls http://localhost:5080
```

## Tests

```bash
dotnet test backend
pnpm --dir web lint
pnpm --dir web build
```

`dotnet test backend` needs Docker running. The suite covers:

- the rule engine and the value objects (`RuleEngineTests`, `SsnTests`, `UsStateTests`, `AddressTests`, `CustomerTests`);
- new and returning customers, denied submissions that write nothing, and rollback when the save fails (`SubmitLoanApplicationTests`);
- the `POST /api/loan-applications` endpoint against real PostgreSQL (`LoanApplicationEndpointTests`);
- outbox order, retry and the `POST`/`PUT` calls to the mock (`OutboxProcessorTests`, `ExternalCustomerHttpClientTests`);
- SSN redaction in logs (`SsnRedactionTests`).

The frontend has lint and build checks only.

## Test data

Fill the other form fields with anything valid (a 5-digit ZIP, for example).

| Case | State | SSN | Result |
|---|---|---|---|
| Approved | `CA` | `555-12-3456` | `/approved`, "This is a new application." |
| Denied by state | `NY` | any SSN not in the blacklist | `/denied`, "We don't offer loans in your state yet." |
| Denied by blacklist | `CA` | `111-11-1111`, `222-22-2222` or `123-45-6789` | `/denied`, "We can't approve this application." |
| Returning customer | `CA` | `555-12-3456` again, with a different amount | `/approved`, "We updated your existing application." Same customer, same application, new amount |
| Both rules | `NY` | `111-11-1111` | `/denied`, both messages above |

A denied submission writes nothing. The blacklist denial returns the neutral code `NOT_ELIGIBLE`, and the page never says "blacklist" or "SSN".

### See what the mock received

```bash
curl http://localhost:4080/customers
```

In PowerShell, use `curl.exe`. After the first approval, `GET /customers` lists the customer. After a returning submission, the same record shows the new amount. Without Docker, use port 4000.

To simulate an outage, make the mock answer 503 to the next 3 requests, then submit an application:

```bash
curl -X POST http://localhost:4080/_admin/fail-next/3
```

The API outbox retries every 2 seconds, in order. The customer shows up in `GET /customers` once the 3 failures are used up.

## Assumptions

Choices the brief left open. UI IDs in parentheses point to the decision table in `web/DESIGN.md` §10.

**Rules**
- The rule engine runs every rule and returns every denial reason, so a user sees them all at once.
- Rules run before the customer lookup. A denied submission writes nothing, and a returning customer who is denied keeps the last approved data.
- The blacklist lives in `appsettings.json` (`Blacklist:Ssns`) behind `ISsnBlacklist`. The API refuses to start if it is empty.
- Denial codes are `STATE_NOT_SERVED` and `NOT_ELIGIBLE`. `NOT_ELIGIBLE` is neutral on purpose, but anyone who probes SSNs can still learn that one is blacklisted. Rate limiting is out of scope. (UI-11)

**Data and transactions**
- One customer owns one application. A repeated SSN updates both. Unique indexes on `Customers.Ssn` and `Applications.CustomerId` enforce it, and a race on the first submission returns 409.
- The SSN accepts `123-45-6789` or 9 digits, is stored as 9 digits, and is masked in `ToString()`. Logs never include it.
- The amount is greater than 0, stored as `numeric(18,2)` with at most 2 decimals. ZIP is 5 digits or ZIP+4. Ids are Guid v7.
- The schema comes from `EnsureCreated`, not migrations.
- The 5 free-text fields (first name, last name, street, city, company) accept up to 100 characters and reject the NUL character with 400.

**Outbox and external service**
- The customer, the application and the outbox row are saved in one `SaveChanges`. A background worker polls every 2 seconds and sends the events later.
- Delivery is at least once, in outbox `Id` order, and the mock upserts by `customerId`.
- The external contract is `POST /customers` to create and `PUT /customers/{customerId}` to update, both with the full snapshot.
- The SSN travels in clear text to the mock, because the brief asks for the same data. This is accepted for a demo.
- The mock adds `GET /customers` and `POST /_admin/fail-next/{n}` to show received data and retries.

**API**
- A denial returns 200 with `status: "Denied"`. Invalid input returns 400 ProblemDetails with errors keyed by field name. A duplicate customer returns 409.
- The API container has no healthcheck, because the chiseled image has no shell.

**UI**
- Own visual identity with no Fundo branding: light and dark themes that follow the system, Lato, Tailwind CSS v4 with three shadcn-style components, CSS-only motion that turns off with reduced motion, English copy. (UI-01, UI-14, UI-22 to UI-27)
- Lato comes from `next/font/google`, so the web build downloads it and `docker compose up --build` needs network access. (UI-24)
- State is a native `<select>` with the 50 states plus DC. (UI-07)
- The server action calls the API using `API_URL`, so the browser needs no CORS and no API address. (UI-16, UI-17)
- Result URLs carry ids and codes only, never personal data. (UI-15, UI-18)
- Field errors show next to the field and in a summary with `role="alert"`. (UI-12)

**Stack and scope**
- Docker Compose is the main path, and a no-Docker path is documented. There is no auth, CI or frontend test suite.
- .NET 10 LTS (.NET 11 is still a release candidate), PostgreSQL 18 and Node 26. Tests use Testcontainers PostgreSQL, never the in-memory provider.
- TypeScript 6.0.3. TypeScript 7 compiles, but `typescript-eslint` does not support it yet, so lint fails.
- Compose maps host ports 5442, 5080, 3080 and 4080 to avoid common local conflicts.

## Not resolved

- Outbox retries have no limit and no dead-letter, so a row that always fails blocks the queue behind it.
- Concurrent updates of one returning customer end with the last write. There is no concurrency token.
- Migrations and `FOR UPDATE SKIP LOCKED` are missing, so the outbox is safe for one API instance only.
- The SSN is stored unencrypted.
- The form has no automated browser test. The five flows were checked by hand.
- The tests need a local .NET SDK. Running them inside a container requires `TESTCONTAINERS_HOST_OVERRIDE`.
- Some 400 messages name the internal field. A zero amount returns `requestedAmount ('0') must be a non-negative and non-zero value.`, and the form shows it as is.
- Npgsql logs `libgssapi_krb5.so.2: cannot open shared object file` when it connects from the chiseled image. It has no effect.
- The outbox query is logged every 2 seconds at Information level (`Microsoft.EntityFrameworkCore.Database.Command`), which fills `docker compose logs`.

Built with help from Claude Code.
