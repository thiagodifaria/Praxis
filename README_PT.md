# Praxis

![Praxis](https://img.shields.io/badge/Praxis-Operational%20Management%20Suite-1F2937?style=for-the-badge&logo=angular&logoColor=white)

**OMS full-stack para vendas, compras, estoque, faturamento e governanca operacional**

[![Angular](https://img.shields.io/badge/Angular-19-DD0031?style=flat&logo=angular&logoColor=white)](https://angular.dev/)
[![.NET](https://img.shields.io/badge/.NET-8-512BD4?style=flat&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8-512BD4?style=flat&logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-316192?style=flat&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D?style=flat&logo=redis&logoColor=white)](https://redis.io/)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-3.13-FF6600?style=flat&logo=rabbitmq&logoColor=white)](https://www.rabbitmq.com/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?style=flat&logo=docker&logoColor=white)](https://www.docker.com/)
[![Tests](https://img.shields.io/badge/Tests-6%20passing-success?style=flat)]()

---

## Documentacao

**Read in English:** [README_EN.md](README_EN.md)  
**Leia em Portugues:** [README_PT.md](README_PT.md)  
**API Reference:** [docs/API.md](docs/API.md)  
**Architecture Reference:** [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)

---

## O que e o Praxis?

Praxis e um OMS pensado para parecer um produto interno real, e nao um CRUD de estudo. O projeto junta um `client-web` em Angular 19, um backend .NET 8 em monolito modular, automacao operacional, observabilidade e fluxos de negocio coerentes entre comercial, suprimentos, estoque, faturamento e financeiro.

Este projeto e um **OMS**, nao um ERP. O centro da modelagem esta em pedido, reserva, expedicao, recebimento, faturamento, recebiveis, payables e leitura operacional.

### Destaques

- `client-web` em Angular 19 com identidade visual propria, selecao de filial e feed realtime
- autenticacao JWT com refresh token
- RBAC com permissoes granulares
- filiais, centros de custo, feature flags e regras de aprovacao
- categorias, fornecedores, produtos e clientes
- pedidos de venda com aprovacao, reserva, expedicao, rejeicao e cancelamento
- pedidos de compra com aprovacao, recebimento parcial ou total e geracao de payable
- estoque com saldos, movimentacoes, ajustes e alertas de baixo estoque
- faturamento com geracao automatica de recebivel
- contas a receber e contas a pagar com baixas
- dashboard, relatorios, auditoria, alertas e fila de aprovacao
- Redis, RabbitMQ, SignalR e Hangfire dentro da mesma stack
- Serilog, OpenTelemetry e health endpoints
- tudo pronto para rodar em Docker Compose

### O que faz este projeto se destacar?

```text
OK Angular 19 com componentes standalone
OK .NET 8 + ASP.NET Core Web API
OK Monolito modular com Domain, Application, Infrastructure e Api
OK PostgreSQL com EF Core migrations e SQL versionado
OK Redis para snapshot do dashboard
OK RabbitMQ para eventos internos e bridge realtime
OK SignalR integrado ao frontend
OK Hangfire em worker dedicado
OK Fluxo OMS multi-filial entre vendas, compras, estoque e billing
OK Centros de custo, regras de aprovacao e feature flags
OK Audit trail, alertas operacionais e inbox de aprovacao
OK OpenTelemetry + Serilog + health endpoints
OK Execucao full Docker incluindo o client-web
```

---

## Inicio Rapido

### Opcao 1: Scripts PowerShell

```powershell
./scripts/build.ps1
./scripts/start.ps1
./scripts/stop.ps1
```

### Opcao 2: Docker Compose

```powershell
cd infra
docker compose --env-file .env up --build -d
```

### Servicos disponiveis

- Web: `http://localhost:4300`
- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- RabbitMQ UI: `http://localhost:15672`
- LGTM / Grafana: `http://localhost:3300`
- PostgreSQL: `localhost:5432`
- Redis: `localhost:6379`

### Credenciais seedadas

```text
admin@praxis.local / Admin@12345
```

---

## Visao Geral da API

| Area | Endpoint | Descricao |
|------|----------|-----------|
| Auth | `POST /api/auth/login` | Autentica e retorna access + refresh token |
| Dashboard | `GET /api/dashboard` | Carrega o snapshot operacional por filial |
| Catalog | `GET /api/catalog/products` | Lista produtos com preco e custo |
| Sales | `POST /api/sales-orders` | Cria pedido de venda em draft |
| Sales | `POST /api/sales-orders/{id}/approve` | Aprova ou reserva um pedido |
| Purchasing | `POST /api/purchase-orders/{id}/receive` | Recebe itens e gera payable |
| Billing | `POST /api/billing/invoices` | Emite fatura a partir de pedido expedido |
| Billing | `POST /api/billing/receivables/{id}/settlements` | Registra baixa de recebivel |
| Reporting | `GET /api/reporting/overview` | Receita, margem e exposicao financeira |
| Operations | `GET /api/operations/approvals` | Le a fila de aprovacao |
| Operations | `GET /api/operations/feature-flags` | Le ou altera modulos ativos |
| Operations | `GET /api/operations/notifications` | Le notificacoes persistidas |
| Realtime | `GET /hubs/notifications` | Hub SignalR para eventos ao vivo |
| Ops | `GET /health/live` | Liveness |
| Ops | `GET /health/ready` | Readiness |

Mais detalhes estao em [docs/API.md](docs/API.md).

---

## Scripts de Automacao

### `scripts/build.ps1`

- builda as imagens Docker de API, worker e web

### `scripts/start.ps1`

- executa `docker compose up -d --build`
- aguarda os servicos dependentes ficarem saudaveis

### `scripts/stop.ps1`

- derruba a stack Compose com seguranca

---

## Contato

**Thiago Di Faria** - thiagodifaria@gmail.com

[![GitHub](https://img.shields.io/badge/GitHub-@thiagodifaria-black?style=flat&logo=github)](https://github.com/thiagodifaria)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-Thiago_Di_Faria-blue?style=flat&logo=linkedin)](https://linkedin.com/in/thiagodifaria)

---

## Agradecimentos

Agradecimentos especiais para:

- ecossistema Angular, .NET e ASP.NET Core
- mantenedores de PostgreSQL, Redis, RabbitMQ, Hangfire e OpenTelemetry
- Docker pela experiencia local de desenvolvimento
- comunidade open source por tras das bibliotecas usadas neste projeto

---

### Se este projeto te ajudou, deixa uma estrela

**Made by [Thiago Di Faria](https://github.com/thiagodifaria)**
