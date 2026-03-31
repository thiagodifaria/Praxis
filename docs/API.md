# API

## Overview

Praxis exposes a REST API focused on OMS execution and an auxiliary SignalR hub for live operational events. The HTTP surface is organized around authentication, master data, sales, purchasing, inventory, billing, reporting and operational governance.

## Local Endpoints

- Web: `http://localhost:4300`
- API: `http://localhost:8080`
- Swagger UI: `http://localhost:8080/swagger`
- SignalR hub: `http://localhost:8080/hubs/notifications`
- RabbitMQ UI: `http://localhost:15672`
- LGTM / Grafana entrypoint: `http://localhost:3300`

## Transport Conventions

### Base routing

- REST routes are rooted under `/api`
- readiness and liveness are exposed under `/health`
- realtime notifications are exposed under `/hubs/notifications`

### Serialization

- JSON is used for request and response bodies
- payloads are emitted in `camelCase`
- enums are exposed as numeric values on the HTTP surface

### Error model

Unhandled and business exceptions are normalized into:

```json
{
  "error": {
    "message": "Human readable message",
    "status": 400
  }
}
```

Mapped exception behavior:

- `NotFoundException` -> `404`
- `ConflictException` -> `409`
- `ValidationException` -> `400`
- `ForbiddenException` -> `403`
- unknown errors -> `500`

## Authentication

Authentication uses JWT bearer tokens signed with HMAC-SHA256.

### Access token behavior

- issuer and audience validation are enabled
- lifetime validation is enabled
- role and permission claims are emitted in the token

### Refresh token behavior

- refresh tokens are persisted
- refresh uses token rotation semantics
- revoked tokens keep replacement metadata

### Seeded user

```text
admin@praxis.local / Admin@12345
```

## Authorization Model

Authorization is permission-driven.

### Main permissions

- `catalog.read`
- `catalog.write`
- `customer.read`
- `customer.write`
- `sales.read`
- `sales.write`
- `purchasing.read`
- `purchasing.write`
- `inventory.read`
- `inventory.write`
- `billing.read`
- `billing.write`
- `dashboard.read`
- `reporting.read`
- `audit.read`
- `ops.manage`
- `configuration.read`
- `configuration.write`
- `notification.read`
- `notification.write`

## Key Endpoints

| Area | Method | Endpoint | Description |
|------|--------|----------|-------------|
| Auth | `POST` | `/api/auth/login` | Authenticate and return access + refresh tokens |
| Auth | `POST` | `/api/auth/refresh` | Rotate the refresh token |
| Dashboard | `GET` | `/api/dashboard` | Load the operational snapshot |
| Catalog | `GET` | `/api/catalog/categories` | List categories |
| Catalog | `GET` | `/api/catalog/suppliers` | List suppliers |
| Catalog | `GET` | `/api/catalog/products` | List products |
| Customers | `GET` | `/api/customers` | List customers |
| Sales | `GET` | `/api/sales-orders` | List sales orders |
| Sales | `POST` | `/api/sales-orders` | Create a sales order |
| Sales | `POST` | `/api/sales-orders/{id}/approve` | Approve a sales order |
| Sales | `POST` | `/api/sales-orders/{id}/reject` | Reject a sales order |
| Sales | `POST` | `/api/sales-orders/{id}/dispatch` | Dispatch a sales order |
| Purchasing | `GET` | `/api/purchase-orders` | List purchase orders |
| Purchasing | `POST` | `/api/purchase-orders` | Create a purchase order |
| Purchasing | `POST` | `/api/purchase-orders/{id}/approve` | Approve a purchase order |
| Purchasing | `POST` | `/api/purchase-orders/{id}/reject` | Reject a purchase order |
| Purchasing | `POST` | `/api/purchase-orders/{id}/receive` | Receive a purchase order |
| Inventory | `GET` | `/api/inventory/warehouses` | List warehouses |
| Inventory | `GET` | `/api/inventory/balances` | List inventory balances |
| Inventory | `GET` | `/api/inventory/movements` | List stock movements |
| Inventory | `POST` | `/api/inventory/adjustments` | Apply manual stock adjustment |
| Billing | `GET` | `/api/billing/invoices` | List invoices |
| Billing | `POST` | `/api/billing/invoices` | Issue invoice from dispatched order |
| Billing | `GET` | `/api/billing/receivables` | List receivables |
| Billing | `POST` | `/api/billing/receivables/{id}/settlements` | Register receivable settlement |
| Billing | `GET` | `/api/billing/payables` | List payables |
| Billing | `POST` | `/api/billing/payables/{id}/settlements` | Register payable settlement |
| Reporting | `GET` | `/api/reporting/overview` | Revenue, margin and exposure overview |
| Reporting | `GET` | `/api/reporting/inventory-turnover` | Inventory turnover snapshot |
| Reporting | `GET` | `/api/reporting/overdue-receivables` | Overdue receivables report |
| Operations | `GET` | `/api/operations/audit` | Read audit entries |
| Operations | `GET` | `/api/operations/alerts` | Read operational alerts |
| Operations | `POST` | `/api/operations/alerts/{id}/resolve` | Resolve alert |
| Settings | `GET` | `/api/operations/branches` | List branches |
| Settings | `GET` | `/api/operations/cost-centers` | List cost centers |
| Settings | `GET` | `/api/operations/feature-flags` | List feature flags |
| Settings | `GET` | `/api/operations/approval-rules` | List approval rules |
| Governance | `GET` | `/api/operations/approvals` | Read approval queue |
| Notifications | `GET` | `/api/operations/notifications` | Read persisted notifications |
| Notifications | `POST` | `/api/operations/notifications/{id}/read` | Mark one notification as read |
| Notifications | `POST` | `/api/operations/notifications/read-all` | Mark the current notification inbox as read |

## Representative Request Contracts

### `LoginRequest`

```json
{
  "email": "admin@praxis.local",
  "password": "Admin@12345"
}
```

### `CreateSalesOrderRequest`

```json
{
  "customerId": "GUID",
  "branchId": "GUID",
  "costCenterId": "GUID",
  "warehouseLocationId": "GUID",
  "notes": "Priority order",
  "items": [
    {
      "productId": "GUID",
      "quantity": 2
    }
  ]
}
```

### `CreatePurchaseOrderRequest`

```json
{
  "supplierId": "GUID",
  "branchId": "GUID",
  "costCenterId": "GUID",
  "warehouseLocationId": "GUID",
  "expectedDeliveryDateUtc": "2026-04-05T00:00:00Z",
  "notes": "Inbound replenishment",
  "items": [
    {
      "productId": "GUID",
      "quantity": 50,
      "unitCost": 4.5
    }
  ]
}
```

### `IssueInvoiceRequest`

```json
{
  "salesOrderId": "GUID",
  "dueDateUtc": "2026-04-15T00:00:00Z",
  "notes": "Invoice for dispatched order"
}
```

### `ApprovalRuleUpsertRequest`

```json
{
  "name": "Purchase orders above 3000",
  "module": 1,
  "branchId": null,
  "minimumAmount": 3000,
  "requiredRoleName": "inventory-manager",
  "description": "Requires inventory management approval.",
  "isActive": true
}
```

## Realtime Behavior

The web client connects to `/hubs/notifications` using SignalR. The backend bridges RabbitMQ topics into live notification messages and the frontend merges that feed with persisted notifications read from `/api/operations/notifications`.

Examples of live operational topics:

- `sales.order.pending-approval`
- `sales.order.rejected`
- `purchasing.order.pending-approval`
- `purchasing.order.rejected`
- `settings.feature-flag.updated`

## Suggested Demo Flow

1. Authenticate with the seeded admin user.
2. Open the `client-web` and review dashboard metrics.
3. Create a sales order and approve or reject it.
4. Dispatch an approved order and issue an invoice.
5. Create a purchase order and receive it.
6. Register settlements for receivables and payables.
7. Inspect alerts, audit entries, notifications and approval rules.
8. Toggle feature flags or create branch-level configuration.

## Related Documents

- [ARCHITECTURE.md](ARCHITECTURE.md)
- [../README.md](../README.md)
- [../README_PT.md](../README_PT.md)
