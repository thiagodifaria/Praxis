# Praxis

![Praxis](https://img.shields.io/badge/Praxis-Operational%20Management%20Suite-1F2937?style=for-the-badge&logo=angular&logoColor=white)

**Full-stack OMS for operational sales, purchasing, inventory, billing and governance**

[![Angular](https://img.shields.io/badge/Angular-19-DD0031?style=flat&logo=angular&logoColor=white)](https://angular.dev/)
[![.NET](https://img.shields.io/badge/.NET-8-512BD4?style=flat&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8-512BD4?style=flat&logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-316192?style=flat&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D?style=flat&logo=redis&logoColor=white)](https://redis.io/)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-3.13-FF6600?style=flat&logo=rabbitmq&logoColor=white)](https://www.rabbitmq.com/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?style=flat&logo=docker&logoColor=white)](https://www.docker.com/)
[![Tests](https://img.shields.io/badge/Tests-6%20passing-success?style=flat)]()

---

## Documentation / Documentacao

**Read in English:** [README_EN.md](README_EN.md)  
**Leia em Portugues:** [README_PT.md](README_PT.md)  
**API Reference:** [docs/API.md](docs/API.md)  
**Architecture Reference:** [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)

---

## What is Praxis?

Praxis is a portfolio-grade OMS built to feel like a real internal operations platform instead of a basic CRUD sample. The project combines a Docker-first Angular control room, a .NET 8 modular monolith, operational automation and realistic business workflows across sales, purchasing, inventory, billing and financial execution.

This is an **OMS**, not an ERP. The core is order flow, stock movement, inbound replenishment, invoicing, receivables, payables and operational visibility.

### Key Highlights

- Angular 19 `client-web` with a custom visual identity, branch-aware navigation and SignalR live feed
- JWT authentication with refresh tokens
- RBAC with granular permission policies
- branches, cost centers, feature flags and approval rules
- categories, suppliers, products and customers
- sales orders with approval, reservation, dispatch, rejection and cancellation
- purchase orders with approval, partial/full receiving and payable generation
- inventory balances, movements, adjustments and low-stock alerting
- invoicing flow with receivable generation
- receivables and payables with settlement registration
- dashboard, reporting, alerts, audit trail and approval queue
- Redis cache, RabbitMQ events, SignalR notifications and Hangfire recurring jobs
- Serilog logging and OpenTelemetry export
- PostgreSQL, Redis, RabbitMQ, LGTM, API, worker and web running together in Docker Compose

### What Makes It Special?

```text
OK Angular 19 control room with standalone components
OK .NET 8 + ASP.NET Core Web API
OK Modular monolith with Domain, Application, Infrastructure and Api
OK PostgreSQL with EF Core migrations and versioned SQL scripts
OK Redis cache for dashboard snapshots
OK RabbitMQ event bus plus realtime notification bridge
OK SignalR live notifications in the frontend
OK Hangfire recurring jobs in a dedicated worker
OK Multi-branch OMS lifecycle across sales, purchasing, inventory and billing
OK Cost centers, approval rules and feature flags
OK Audit trail, operational alerts and approval inbox
OK OpenTelemetry + Serilog + health endpoints
OK Full Docker-first execution including the web client
```

---

## Quick Start

### Option 1: PowerShell Scripts

```powershell
./scripts/build.ps1
./scripts/start.ps1
./scripts/stop.ps1
```

### Option 2: Docker Compose

```powershell
cd infra
docker compose --env-file .env up --build -d
```

### Available Services

- Web: `http://localhost:4300`
- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- RabbitMQ UI: `http://localhost:15672`
- LGTM / Grafana entrypoint: `http://localhost:3300`
- PostgreSQL: `localhost:5432`
- Redis: `localhost:6379`

### Seed Credentials

```text
admin@praxis.local / Admin@12345
```

---

## API Overview

| Area | Endpoint | Description |
|------|----------|-------------|
| Auth | `POST /api/auth/login` | Authenticate and return access + refresh tokens |
| Dashboard | `GET /api/dashboard` | Load the branch-aware operational snapshot |
| Catalog | `GET /api/catalog/products` | List products with price and standard cost |
| Sales | `POST /api/sales-orders` | Create a draft sales order |
| Sales | `POST /api/sales-orders/{id}/approve` | Approve or reserve an order |
| Purchasing | `POST /api/purchase-orders/{id}/receive` | Receive items and generate payable |
| Billing | `POST /api/billing/invoices` | Issue invoice from a dispatched order |
| Billing | `POST /api/billing/receivables/{id}/settlements` | Register receivable settlement |
| Reporting | `GET /api/reporting/overview` | Revenue, margin and exposure overview |
| Operations | `GET /api/operations/approvals` | Read the approval queue |
| Operations | `GET /api/operations/feature-flags` | Read or update module availability |
| Operations | `GET /api/operations/notifications` | Read persisted notifications |
| Realtime | `GET /hubs/notifications` | SignalR hub for live events |
| Ops | `GET /health/live` | Liveness endpoint |
| Ops | `GET /health/ready` | Readiness endpoint |

More details are available in [docs/API.md](docs/API.md).

---

## Automation Scripts

### `scripts/build.ps1`

- builds the Docker images for API, worker and web

### `scripts/start.ps1`

- runs `docker compose up -d --build`
- waits for dependent services to become healthy

### `scripts/stop.ps1`

- stops the Compose stack cleanly

---

## Contact

**Thiago Di Faria** - thiagodifaria@gmail.com

[![GitHub](https://img.shields.io/badge/GitHub-@thiagodifaria-black?style=flat&logo=github)](https://github.com/thiagodifaria)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-Thiago_Di_Faria-blue?style=flat&logo=linkedin)](https://linkedin.com/in/thiagodifaria)

---

## Acknowledgments

Special thanks to:

- the Angular, .NET and ASP.NET Core ecosystem
- PostgreSQL, Redis, RabbitMQ, Hangfire and OpenTelemetry maintainers
- Docker for the local developer experience
- open-source contributors behind the libraries used in this project

---

### Star this project if you find it useful

**Made by [Thiago Di Faria](https://github.com/thiagodifaria)**
