# Fundo Loan Flow

A loan application form posts to a .NET API that runs a rule engine and stores the result in PostgreSQL with a transactional outbox.
A background worker then syncs approved applications to a mock external service over HTTP.

## Stack

.NET 10, EF Core 10 + Npgsql, PostgreSQL 18, xUnit v3 + Testcontainers, Next.js 16 + React 19, Node 26, Docker Compose.

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
└── compose.yaml                      db (PostgreSQL 18) + api + web + external-mock.
```

Setup instructions arrive with the first runnable version.
