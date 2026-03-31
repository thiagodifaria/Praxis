# ARCHITECTURE

## Purpose

This document describes the current architecture of Praxis V3. The system is intentionally split into a Docker-first Angular frontend plus a .NET modular monolith with a dedicated worker, keeping the repository cohesive while preserving strong module boundaries.

Praxis is modeled as an **OMS**, not an ERP. The main concern is order flow across sales, purchasing, inventory, billing, receivables, payables and operational control.

## Architectural Style

### High-level style

Praxis follows a full-stack modular-monolith approach:

```text
client-web (Angular 19)
  -> REST / SignalR
    -> Praxis.Api
      -> Praxis.Application
        -> Praxis.Domain
          -> Praxis.Infrastructure
            -> PostgreSQL / Redis / RabbitMQ / OpenTelemetry

Praxis.Worker
  -> Hangfire recurring jobs
  -> RabbitMQ processing
  -> same Application / Domain / Infrastructure stack
```

This structure was chosen to balance:

- a real product feel instead of an example-app shape
- fast iteration in a single repository
- simple Docker-first local execution
- explicit business-rule ownership
- room for future bounded-context extraction if needed

## Solution Layout

```text
client-web/
  src/
    app/
      core/
      layout/
      pages/
      shared/
docs/
  API.md
  ARCHITECTURE.md
infra/
  docker-compose.yml
  .env.example
  server/csharp/
  server/web/
scripts/
  build.ps1
  start.ps1
  stop.ps1
service-api/
  service-csharp/
    Praxis.sln
    src/
      Praxis.Api
      Praxis.Application
      Praxis.Domain
      Praxis.Infrastructure
      Praxis.Worker
    tests/
      Praxis.Domain.Tests
      Praxis.IntegrationTests
  service-postgresql/
    migrations/
    seeds/
```

## Frontend Architecture

### `client-web`

The web layer is a standalone Angular 19 application organized around:

- `core`
  - `ApiService`
  - `AuthStore`
  - auth guard and interceptor
  - realtime SignalR service
- `layout`
  - `ShellComponent` for sidebar, branch selector and notification drawer
- `pages`
  - login
  - dashboard
  - catalog
  - customers
  - sales
  - purchasing
  - inventory
  - billing
  - reporting
  - operations
  - settings
- `shared`
  - status labels, value formatters and presentation helpers

### Frontend responsibilities

- branch-aware navigation and module visibility
- authentication state and token forwarding
- REST consumption for all OMS modules
- SignalR live feed for operational notifications
- command-center style UI for the V3 operational suite

## Backend Layer Responsibilities

### `Praxis.Domain`

Owns the core business model:

- entities
- enums
- invariants
- state transitions

Examples:

- `SalesOrder` approval and dispatch rules
- `PurchaseOrder` approval and receipt lifecycle
- `InventoryBalance` reservation and dispatch invariants
- `Invoice`, `Receivable` and `Payable` settlement rules
- `Branch`, `CostCenter`, `ApprovalRule` and `RealtimeNotification`

### `Praxis.Application`

Owns use-case orchestration:

- application services
- DTOs and request/response models
- cross-module orchestration
- dashboard/report aggregation
- approval and policy enforcement
- audit and event publishing

### `Praxis.Infrastructure`

Owns technical implementation:

- `PraxisDbContext`
- EF Core mappings and migrations
- JWT and password hashing
- Redis dashboard cache
- RabbitMQ event bus and consumers
- SignalR realtime bridge support
- Hangfire job registration
- seed and backfill routines

### `Praxis.Api`

Owns the HTTP runtime:

- controllers
- Swagger
- exception middleware
- authentication and authorization
- CORS for web clients
- SignalR hub mapping
- health endpoints

### `Praxis.Worker`

Owns background execution:

- Hangfire server
- recurring jobs
- asynchronous messaging workloads
- same persistence and telemetry stack as the API

## Runtime Topology

### Docker services

- `client-web`
  - Angular bundle served by Nginx
  - SPA routing via `try_files`
- `service-csharp`
  - ASP.NET Core API
  - Swagger
  - SignalR hub
  - health endpoints
- `service-csharp-worker`
  - Hangfire server
  - RabbitMQ processing
- `service-postgresql`
  - main relational database
- `service-redis`
  - dashboard cache
- `service-rabbitmq`
  - internal broker
- `service-otel-lgtm`
  - local telemetry sink and visualization

### Ports

- Web: `4300`
- API: `8080`
- PostgreSQL: `5432`
- Redis: `6379`
- RabbitMQ: `5672`
- RabbitMQ UI: `15672`
- LGTM / Grafana: `3300`

## Core V3 Capabilities

### Governance and configuration

- branches
- cost centers
- approval rules
- feature flags by module
- approval decision queue
- persisted notifications

### Realtime

- RabbitMQ events are persisted as `RealtimeNotification`
- the API hosts `/hubs/notifications`
- the frontend merges persisted notifications with live SignalR events

### Operational lifecycle

```text
Sales order
  -> approval policy evaluation
  -> reservation
  -> dispatch
  -> invoice
  -> receivable

Purchase order
  -> approval policy evaluation
  -> partial or full receipt
  -> stock movement
  -> payable
```

## Startup Flow

### API startup

`Program.cs` in `Praxis.Api` performs:

1. Serilog configuration
2. dependency registration through `AddPraxisPlatform`
3. API runtime registration through `AddPraxisApiRuntime`
4. controller, SignalR, CORS and Swagger setup
5. permission policy registration
6. OpenTelemetry registration
7. database bootstrap and seed execution
8. middleware wiring
9. controller, hub and health endpoint mapping

### Worker startup

`Program.cs` in `Praxis.Worker` performs:

1. shared platform registration
2. worker runtime registration
3. telemetry and logging setup
4. database bootstrap execution
5. Hangfire recurring job registration
6. worker host startup

### Frontend startup

The web layer is built into static assets and served by Nginx. At runtime it connects to:

- `http://localhost:8080/api`
- `http://localhost:8080/hubs/notifications`

The API currently allows browser origins:

- `http://localhost:4200`
- `http://localhost:4300`

## Persistence and Modeling

### Primary database

PostgreSQL is the source of truth for:

- identity and access
- catalog and customers
- sales and purchase orders
- inventory balances and movements
- invoices, receivables and payables
- audit entries and alerts
- branches, cost centers, feature flags and approval rules
- realtime notification persistence

### Cache and async strategy

- Redis caches dashboard snapshots
- RabbitMQ transports internal operational events
- Hangfire executes recurring scans and refresh jobs
- SignalR broadcasts live notifications to the web UI

## Observability

### Logging

- Serilog request logging in the API
- console logging in API and worker

### Telemetry

- OpenTelemetry traces and metrics
- OTLP export to the LGTM container

### Health

- `/health/live` for liveness
- `/health/ready` for readiness across PostgreSQL, Redis and RabbitMQ

## Testing Strategy

Current automated validation covers:

- domain rules for sales and financial transitions
- integration tests for dashboard generation
- integration tests for reporting aggregates
- Angular production build for the web layer
- Docker build validation for the `client-web` image

## Design Tradeoffs

### Why a modular monolith plus worker?

Because it keeps:

- deployment simple
- the repository cohesive
- cross-module workflows easy to reason about
- portfolio review fast and realistic

while still giving clear ownership to each module.

### Why a custom frontend now?

Because V3 is intended to feel like a complete product. The backend remains the operational core, but the Angular client makes the governance, reporting and approval workflows tangible.

## Related Documents

- [API.md](API.md)
- [../README.md](../README.md)
- [../README_PT.md](../README_PT.md)
