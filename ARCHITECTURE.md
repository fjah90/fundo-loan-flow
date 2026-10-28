# Architecture

A loan application flow: a Next.js form, a .NET API that runs a rule engine, PostgreSQL persistence with a transactional outbox, and a background worker that syncs approved applications to a mock external service over HTTP.

```
Next.js (web) ──POST /api/loan-applications──▶ .NET API ──▶ PostgreSQL 18
                                                  │   (customer + application + outbox row: one transaction)
                                                  ▼
                                      OutboxProcessor (BackgroundService)
                                                  │ POST /customers | PUT /customers/{id}
                                                  ▼
                                      external-mock (Node, no deps)
```

## 1. Project structure

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
└── compose.yaml                      db (PostgreSQL 18) + api + web + external-mock.
```

Stack: .NET 10 LTS, EF Core 10 + Npgsql, PostgreSQL 18, xUnit v3 + Testcontainers, Next.js 16 + React 19, Node 26, Docker Compose. These were the latest stable versions on 2026-10-27.

Dependencies point inward: `Api → Infrastructure → Application → Domain`. Project references enforce it at compile time. Api references Infrastructure **only** in `Program.cs` to register services. The controller only sees Application types. Domain and Application never reference EF Core, ASP.NET or `HttpClient`.

| Layer | Owns | Does not own |
|---|---|---|
| Domain | `Customer` (aggregate root) and its single `LoanApplication`; value objects `Ssn`, `Address`, `UsState`; `LoanApplicant` (rule input), `Decision`, `DenialReason`; `IDenyRule`, `RuleEngine`, the two rules | Persistence, HTTP, configuration |
| Application | `SubmitLoanApplication` use case; `CustomerSyncRequested` integration event; `SyncCustomerWithExternalService` handler; ports | How ports are implemented |
| Infrastructure | `LoanFlowDbContext`, repository, unit of work, outbox publisher and table, `OutboxProcessor`, typed `HttpClient`, blacklist from configuration | Business decisions |
| Api | `LoanApplicationsController` (request → command → response), ProblemDetails, DI wiring | Rules, transactions |

**Aggregate:** `Customer` owns its `LoanApplication` because the brief allows one application per customer, and both always change together. `LoanApplication` keeps `CustomerId` because the brief asks for that field.

**Ports.** Each port marks a boundary the brief says must be replaceable, or a seam a test needs:

| Port | Layer | Why it exists | Implementation |
|---|---|---|---|
| `ISsnBlacklist` | Domain | The blacklist source can change (config today, a service later). Faked in rule tests. | `ConfigurationSsnBlacklist` |
| `ICustomerRepository` | Application | Database is replaceable. | `EfCustomerRepository` |
| `IUnitOfWork` | Application | One commit for customer, application and event. | `LoanFlowDbContext` |
| `IIntegrationEventPublisher` | Application | Messaging is replaceable. A broker would be a new implementation here. | `OutboxEventPublisher` (adds a row to the same `DbContext`) |
| `IExternalCustomerService` | Application | HTTP client is replaceable. Faked in outbox tests. | `ExternalCustomerHttpClient` |

## 2. Rule engine

```csharp
public interface IDenyRule
{
    DenialReason? Evaluate(LoanApplicant applicant); // null = rule does not deny
}
```

- `RuleEngine` receives `IEnumerable<IDenyRule>` from DI, runs **all** rules, and returns `Decision.Approved` or `Decision.Denied(reasons)`.
- Rules:
  - `NewYorkStateRule` returns `STATE_NOT_SERVED`.
  - `BlacklistedSsnRule` returns `NOT_ELIGIBLE`, using `ISsnBlacklist`. The neutral code means neither the API nor the UI reveals that an SSN is flagged.
- **Adding a rule:** write one class that implements `IDenyRule` and register it with one line in DI. No existing rule changes, and neither does the engine.
- Rules run before the customer lookup. The blacklist is the "check the SSN" step from the brief.
- **Denied writes nothing.** A returning customer who is denied later keeps the last approved data, and nothing is sent to the external service.

## 3. Submission flow and transaction

`SubmitLoanApplication.Handle(command)`:

1. Build the value objects. Invalid input returns 400 ProblemDetails before any rule runs.
2. `RuleEngine.Evaluate`. If any rule denies, return `Denied(reasons)`.
3. `ICustomerRepository.GetBySsnAsync(ssn)`:
   - Not found: `Customer.Create(...)` creates the customer and its application.
   - Found: `customer.UpdateFrom(...)` updates the personal data and the existing application's amount. No second customer or application is created.
4. `IIntegrationEventPublisher.Publish(CustomerSyncRequested { Kind = Created | Updated, snapshot })`. This stages an outbox row.
5. `IUnitOfWork.CommitAsync()` calls `SaveChanges` once. EF Core wraps one `SaveChanges` in a single database transaction ([EF Core docs: Transactions](https://learn.microsoft.com/ef/core/saving/transactions)).

**Why an outbox satisfies "publishing is part of the unit of work".** The brief asks for two things at once:
- Saving the customer, saving the application and publishing the event must roll back together.
- The event must be processed in the background, not inside the HTTP request.

Sending HTTP inside the transaction would break both: a remote call cannot be rolled back, and it would run inside the request. So *publishing* here means recording the event durably in the same transaction as the data. *Delivery* is a separate background step that runs at least once. If the commit fails, the event does not exist.

| Failure | Result |
|---|---|
| A deny rule matches | Nothing written. 200 with `status: "Denied"` and reason codes. |
| Database error during commit | Transaction rolls back: no customer, no application, no outbox row, so no event. API returns 500 ProblemDetails. |
| Two concurrent *first* submissions with the same SSN | The unique index on `Customers.Ssn` rejects the second insert with PostgreSQL error `23505`. Infrastructure translates it to `DuplicateCustomerException`, and the API returns 409. Resubmitting goes through the update path. |
| External service down | Data is already committed. The outbox row stays pending and the worker retries. |
| Worker crashes after the mock answered but before marking the row | The row is sent again. The mock upserts by `customerId`, so the retry is harmless. |

The database backs two invariants: unique `Customers.Ssn` and unique `Applications.CustomerId`, so each customer has one application.

## 4. Background event and external service

- `OutboxProcessor` is a `BackgroundService` inside the API process.
  - Every 2 s (`Outbox:PollIntervalSeconds`), it calls `ProcessPendingAsync()`.
  - That method reads pending rows ordered by their **identity key** (a monotonic `bigint`, not a timestamp) and runs `SyncCustomerWithExternalService` for each one.
  - `Outbox:Enabled=false` turns the loop off. Endpoint tests set it, and outbox tests call `ProcessPendingAsync()` directly, with no sleeps.
- `Created` is sent as `POST /customers` and `Updated` as `PUT /customers/{customerId}`.
- The payload is the full snapshot: customer data, SSN included, plus `application: { id, requestedAmount }`. The brief asks to send "the same data". Sending the SSN in the clear to a local mock is an accepted demo risk.
- On success, `ProcessedAt` is set as a UTC `timestamptz`.
- On failure, `Attempts` increases, `LastError` is saved, and the batch **stops**, so an update is never delivered before its create. The next poll retries.
- Delivery is **at least once**. The mock is idempotent by `customerId`, and our `customerId` is the external id, so we never store an id from the mock.
- `external-mock`:
  - Keeps records in memory and logs method, path and `customerId` only, never the SSN.
  - Exposes `GET /customers` for the demo.
  - Exposes `POST /_admin/fail-next/{n}` to simulate an outage and show the retries.

## 5. Trade-offs (left out on purpose)

| Left out | Why | Add when |
|---|---|---|
| Message broker (RabbitMQ, MassTransit) | An outbox in the same database gives atomicity with no extra infrastructure. `IIntegrationEventPublisher` is the swap point. | Several consumers or services need the event. |
| SQLite | It needs no extra container. PostgreSQL behaves like production (concurrency, unique-violation codes), and Testcontainers lets the tests run against the same engine. | — |
| EF migrations | `EnsureCreated` is enough for one schema version. If the schema changes, reset with `docker compose down -v`. | The schema has to evolve with real data. |
| Max attempts and dead-letter | A row that keeps failing blocks the queue. The mock only fails when asked to, and `LastError` stays visible. | Real traffic against a partner that can return permanent 4xx errors. |
| `FOR UPDATE SKIP LOCKED` in the outbox | A single API instance runs a single worker, so rows are never claimed twice. | More than one API instance. |
| Optimistic concurrency on the update path | Two simultaneous submissions for the same returning customer: the last write wins. | Real concurrent edits (an `xmin` concurrency token is one line). |
| Mock persistence | If the mock restarts, it loses its data, and a later `PUT` upserts an unknown id. A real service would return 404 and need a re-create. | A real partner integration. |
| API container healthcheck | The chiseled runtime image has no shell or curl. `web` only calls the API on submit. | Orchestrators that need readiness probes. |
| SSN encryption at rest | Out of scope for a local demo. The SSN is masked in our logs and never put in URLs. | Before any real data. |
| Persisting denied applications | Not required. Deny reasons are returned and logged. | Audit or analytics requirements. |
| Auth, CI | Auth is excluded by the brief. Docker Compose is included because it reduces "run everything, database included" to one command for the reviewer. | Shared environments. |
| Frontend tests | The brief asks for rule engine, returning customer and endpoint coverage. The UI is checked in the video. | The UI grows beyond one form. |
