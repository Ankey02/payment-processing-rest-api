# Payment Processing REST API

A payment API built with C# / .NET 9 and PostgreSQL. It covers the authorize, capture and refund lifecycle with the safety guarantees a real payments system needs: **idempotent requests**, **race-safe state transitions**, a **double-entry ledger**, and **per-merchant API-key authentication**.

## Features

- Authorize, capture, void, and (partial) refund payments
- Idempotency keys: retrying a request never creates a second payment
- Optimistic concurrency (PostgreSQL `xmin`): parallel captures can't double-charge
- Double-entry ledger written in the same DB transaction as each payment change
- API-key authentication per merchant (keys stored as SHA-256 hashes, merchants only see their own data)
- Paged, filterable payment list (`status`, `from`, `to`)
- Correlation IDs, structured logging (Serilog), rate limiting, `/health` endpoint
- Interactive API docs (Scalar) at `/scalar/v1` in Development
- Clean Architecture: Domain / Application / Infrastructure / Api

## Architecture

~~~mermaid
flowchart LR
    Client --> Api[PayApi.Api<br/>Controllers, auth, middleware]
    Api --> App[PayApi.Application<br/>Services, contracts]
    App --> Domain[PayApi.Domain<br/>Payment, state machine, ledger]
    Infra[PayApi.Infrastructure<br/>EF Core, migrations] --> App
    Infra --> DB[(PostgreSQL)]
~~~

## Run it locally

Prerequisites: .NET 9 SDK, Docker Desktop.

~~~bash
docker compose up -d
dotnet tool install --global dotnet-ef --version 9.0.1
dotnet ef database update -p src/PayApi.Infrastructure -s src/PayApi.Api
dotnet run --project src/PayApi.Api --urls http://localhost:5080
~~~

Then open http://localhost:5080/scalar/v1 for the interactive docs.

The credentials in `docker-compose.yml`, `appsettings.json` and `appsettings.Development.json` (including the admin key) are for a local-only development setup.

## Authentication

All `/payments` endpoints require an `X-Api-Key` header. Create a merchant (and get its key, shown once) with the admin endpoint:

~~~bash
curl -X POST http://localhost:5080/admin/merchants \
  -H "Content-Type: application/json" \
  -H "X-Admin-Key: dev-admin-key-change-me" \
  -d '{"name":"Demo Cafe"}'
~~~

## API

| Method | Path | Description |
|---|---|---|
| POST | `/payments` | Authorize (requires `Idempotency-Key` header) |
| POST | `/payments/{id}/capture` | Capture an authorized payment |
| POST | `/payments/{id}/void` | Void an authorized payment |
| POST | `/payments/{id}/refund` | Full or partial refund |
| GET | `/payments/{id}` | Get a payment |
| GET | `/payments?status=&from=&to=&page=&pageSize=` | List payments |
| GET | `/payments/{id}/ledger` | Ledger entries for a payment |
| POST | `/admin/merchants` | Create a merchant and API key (admin key) |
| GET | `/health` | Health check |

### Example

~~~bash
curl -i -X POST http://localhost:5080/payments \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: order-1001" \
  -H "X-Api-Key: pk_test_YOUR_KEY" \
  -d '{"amount":2500,"currency":"usd"}'
~~~

Run it twice: the second response returns the same payment with `Idempotent-Replayed: true`.

Payment states: `Authorized -> Captured -> PartiallyRefunded -> Refunded`, or `Authorized -> Voided`. Invalid transitions return `409`.

## Design decisions

- **Amounts are integers (cents).** Avoids floating-point and decimal rounding bugs.
- **Idempotency keys are stored with a request hash and the saved response.** Same key and same body replays the response; same key and different body returns `422`. A unique key per merchant means parallel duplicates collide in the database and the loser returns the winner's response.
- **The state machine lives in the domain entity**, so no controller or service can skip a rule.
- **Optimistic concurrency via `xmin`** instead of locks: simple, fast, and a conflicting writer is rejected rather than blocked.
- **Double-entry ledger**: every capture or refund writes a balanced debit and credit in the same transaction as the payment update, so money movement is auditable.
- **API keys are hashed (SHA-256)** and shown once at creation, so a database leak doesn't expose usable keys.

## Tests

~~~bash
dotnet test
~~~

- Unit tests for the payment state machine (xUnit)
- Integration tests against a real PostgreSQL container (Testcontainers), including:
  - 20 parallel authorize requests with the same key create exactly one payment
  - 50 parallel capture requests: exactly one succeeds, 49 are rejected, and the ledger has exactly two entries
  - Missing or wrong API key returns 401; one merchant cannot read another merchant's payment

Docker must be running for the integration tests.

## Roadmap

- Payment gateway abstraction with a Stripe test-mode adapter
- Dockerfile and CI pipeline (GitHub Actions)

## Query performance

Measured locally with `EXPLAIN ANALYZE` on 500,000 synthetic payments, for the query "latest 20 payments for a merchant in the last 7 days":

| | Plan | Execution time |
|---|---|---|
| Without index | Parallel Seq Scan | ~12 ms |
| With composite index on (MerchantId, CreatedAt) | Index Scan Backward | ~0.12 ms |

That is roughly 100x faster, and the gap widens as the table grows.
