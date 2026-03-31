import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AdjustInventoryRequest,
  ApprovalQueueItem,
  ApprovalRule,
  ApprovalRuleUpsertRequest,
  AuditEntry,
  Branch,
  BranchUpsertRequest,
  Category,
  CategoryUpsertRequest,
  CostCenter,
  CostCenterUpsertRequest,
  CreatePurchaseOrderRequest,
  CreateSalesOrderRequest,
  Customer,
  CustomerUpsertRequest,
  DashboardSnapshot,
  FeatureFlag,
  FeatureFlagUpdateRequest,
  FinancialSettlementRequest,
  InventoryBalance,
  InventoryTurnoverReport,
  Invoice,
  IssueInvoiceRequest,
  LoginRequest,
  LoginResponse,
  NotificationItem,
  OperationalAlert,
  OrderStatusRequest,
  OverdueReceivablesReport,
  Payable,
  Product,
  ProductUpsertRequest,
  PurchaseOrder,
  Receivable,
  ReceivePurchaseOrderRequest,
  RefreshTokenRequest,
  ReportingOverview,
  SalesOrder,
  StockMovement,
  Supplier,
  SupplierUpsertRequest,
  Warehouse,
} from './app.models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = 'http://localhost:8080/api';
  readonly hubUrl = 'http://localhost:8080/hubs/notifications';

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiBaseUrl}/auth/login`, request);
  }

  refresh(request: RefreshTokenRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiBaseUrl}/auth/refresh`, request);
  }

  getDashboard(branchId?: string | null, forceRefresh = false): Observable<DashboardSnapshot> {
    return this.http.get<DashboardSnapshot>(`${this.apiBaseUrl}/dashboard`, {
      params: this.toParams({ branchId, forceRefresh }),
    });
  }

  listCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.apiBaseUrl}/catalog/categories`);
  }

  createCategory(request: CategoryUpsertRequest): Observable<Category> {
    return this.http.post<Category>(`${this.apiBaseUrl}/catalog/categories`, request);
  }

  updateCategory(id: string, request: CategoryUpsertRequest): Observable<Category> {
    return this.http.put<Category>(`${this.apiBaseUrl}/catalog/categories/${id}`, request);
  }

  listSuppliers(): Observable<Supplier[]> {
    return this.http.get<Supplier[]>(`${this.apiBaseUrl}/catalog/suppliers`);
  }

  createSupplier(request: SupplierUpsertRequest): Observable<Supplier> {
    return this.http.post<Supplier>(`${this.apiBaseUrl}/catalog/suppliers`, request);
  }

  updateSupplier(id: string, request: SupplierUpsertRequest): Observable<Supplier> {
    return this.http.put<Supplier>(`${this.apiBaseUrl}/catalog/suppliers/${id}`, request);
  }

  listProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.apiBaseUrl}/catalog/products`);
  }

  createProduct(request: ProductUpsertRequest): Observable<Product> {
    return this.http.post<Product>(`${this.apiBaseUrl}/catalog/products`, request);
  }

  updateProduct(id: string, request: ProductUpsertRequest): Observable<Product> {
    return this.http.put<Product>(`${this.apiBaseUrl}/catalog/products/${id}`, request);
  }

  listCustomers(): Observable<Customer[]> {
    return this.http.get<Customer[]>(`${this.apiBaseUrl}/customers`);
  }

  createCustomer(request: CustomerUpsertRequest): Observable<Customer> {
    return this.http.post<Customer>(`${this.apiBaseUrl}/customers`, request);
  }

  updateCustomer(id: string, request: CustomerUpsertRequest): Observable<Customer> {
    return this.http.put<Customer>(`${this.apiBaseUrl}/customers/${id}`, request);
  }

  listSalesOrders(branchId?: string | null, costCenterId?: string | null, approvalStatus?: number | null): Observable<SalesOrder[]> {
    return this.http.get<SalesOrder[]>(`${this.apiBaseUrl}/sales-orders`, {
      params: this.toParams({ branchId, costCenterId, approvalStatus }),
    });
  }

  createSalesOrder(request: CreateSalesOrderRequest): Observable<SalesOrder> {
    return this.http.post<SalesOrder>(`${this.apiBaseUrl}/sales-orders`, request);
  }

  approveSalesOrder(id: string): Observable<SalesOrder> {
    return this.http.post<SalesOrder>(`${this.apiBaseUrl}/sales-orders/${id}/approve`, {});
  }

  rejectSalesOrder(id: string, request: OrderStatusRequest): Observable<SalesOrder> {
    return this.http.post<SalesOrder>(`${this.apiBaseUrl}/sales-orders/${id}/reject`, request);
  }

  dispatchSalesOrder(id: string): Observable<SalesOrder> {
    return this.http.post<SalesOrder>(`${this.apiBaseUrl}/sales-orders/${id}/dispatch`, {});
  }

  cancelSalesOrder(id: string, request: OrderStatusRequest): Observable<SalesOrder> {
    return this.http.post<SalesOrder>(`${this.apiBaseUrl}/sales-orders/${id}/cancel`, request);
  }

  listPurchaseOrders(branchId?: string | null, costCenterId?: string | null, approvalStatus?: number | null): Observable<PurchaseOrder[]> {
    return this.http.get<PurchaseOrder[]>(`${this.apiBaseUrl}/purchase-orders`, {
      params: this.toParams({ branchId, costCenterId, approvalStatus }),
    });
  }

  createPurchaseOrder(request: CreatePurchaseOrderRequest): Observable<PurchaseOrder> {
    return this.http.post<PurchaseOrder>(`${this.apiBaseUrl}/purchase-orders`, request);
  }

  approvePurchaseOrder(id: string): Observable<PurchaseOrder> {
    return this.http.post<PurchaseOrder>(`${this.apiBaseUrl}/purchase-orders/${id}/approve`, {});
  }

  rejectPurchaseOrder(id: string, request: OrderStatusRequest): Observable<PurchaseOrder> {
    return this.http.post<PurchaseOrder>(`${this.apiBaseUrl}/purchase-orders/${id}/reject`, request);
  }

  receivePurchaseOrder(id: string, request: ReceivePurchaseOrderRequest): Observable<PurchaseOrder> {
    return this.http.post<PurchaseOrder>(`${this.apiBaseUrl}/purchase-orders/${id}/receive`, request);
  }

  cancelPurchaseOrder(id: string, request: OrderStatusRequest): Observable<PurchaseOrder> {
    return this.http.post<PurchaseOrder>(`${this.apiBaseUrl}/purchase-orders/${id}/cancel`, request);
  }

  listWarehouses(branchId?: string | null): Observable<Warehouse[]> {
    return this.http.get<Warehouse[]>(`${this.apiBaseUrl}/inventory/warehouses`, {
      params: this.toParams({ branchId }),
    });
  }

  listBalances(branchId?: string | null): Observable<InventoryBalance[]> {
    return this.http.get<InventoryBalance[]>(`${this.apiBaseUrl}/inventory/balances`, {
      params: this.toParams({ branchId }),
    });
  }

  listMovements(branchId?: string | null): Observable<StockMovement[]> {
    return this.http.get<StockMovement[]>(`${this.apiBaseUrl}/inventory/movements`, {
      params: this.toParams({ branchId }),
    });
  }

  adjustInventory(request: AdjustInventoryRequest): Observable<InventoryBalance> {
    return this.http.post<InventoryBalance>(`${this.apiBaseUrl}/inventory/adjustments`, request);
  }

  listInvoices(branchId?: string | null, costCenterId?: string | null): Observable<Invoice[]> {
    return this.http.get<Invoice[]>(`${this.apiBaseUrl}/billing/invoices`, {
      params: this.toParams({ branchId, costCenterId }),
    });
  }

  issueInvoice(request: IssueInvoiceRequest): Observable<Invoice> {
    return this.http.post<Invoice>(`${this.apiBaseUrl}/billing/invoices`, request);
  }

  cancelInvoice(id: string, request: OrderStatusRequest): Observable<Invoice> {
    return this.http.post<Invoice>(`${this.apiBaseUrl}/billing/invoices/${id}/cancel`, request);
  }

  listReceivables(branchId?: string | null, costCenterId?: string | null): Observable<Receivable[]> {
    return this.http.get<Receivable[]>(`${this.apiBaseUrl}/billing/receivables`, {
      params: this.toParams({ branchId, costCenterId }),
    });
  }

  settleReceivable(id: string, request: FinancialSettlementRequest): Observable<Receivable> {
    return this.http.post<Receivable>(`${this.apiBaseUrl}/billing/receivables/${id}/settlements`, request);
  }

  listPayables(branchId?: string | null, costCenterId?: string | null): Observable<Payable[]> {
    return this.http.get<Payable[]>(`${this.apiBaseUrl}/billing/payables`, {
      params: this.toParams({ branchId, costCenterId }),
    });
  }

  settlePayable(id: string, request: FinancialSettlementRequest): Observable<Payable> {
    return this.http.post<Payable>(`${this.apiBaseUrl}/billing/payables/${id}/settlements`, request);
  }

  getReportingOverview(fromUtc?: string | null, toUtc?: string | null, branchId?: string | null, costCenterId?: string | null): Observable<ReportingOverview> {
    return this.http.get<ReportingOverview>(`${this.apiBaseUrl}/reporting/overview`, {
      params: this.toParams({ fromUtc, toUtc, branchId, costCenterId }),
    });
  }

  getInventoryTurnover(fromUtc?: string | null, toUtc?: string | null, branchId?: string | null): Observable<InventoryTurnoverReport> {
    return this.http.get<InventoryTurnoverReport>(`${this.apiBaseUrl}/reporting/inventory-turnover`, {
      params: this.toParams({ fromUtc, toUtc, branchId }),
    });
  }

  getOverdueReceivables(branchId?: string | null, costCenterId?: string | null): Observable<OverdueReceivablesReport> {
    return this.http.get<OverdueReceivablesReport>(`${this.apiBaseUrl}/reporting/overdue-receivables`, {
      params: this.toParams({ branchId, costCenterId }),
    });
  }

  listAuditEntries(eventType?: string | null, entityName?: string | null, limit = 100): Observable<AuditEntry[]> {
    return this.http.get<AuditEntry[]>(`${this.apiBaseUrl}/operations/audit`, {
      params: this.toParams({ eventType, entityName, limit }),
    });
  }

  listAlerts(branchId?: string | null, openOnly = true): Observable<OperationalAlert[]> {
    return this.http.get<OperationalAlert[]>(`${this.apiBaseUrl}/operations/alerts`, {
      params: this.toParams({ branchId, openOnly }),
    });
  }

  resolveAlert(id: string): Observable<OperationalAlert> {
    return this.http.post<OperationalAlert>(`${this.apiBaseUrl}/operations/alerts/${id}/resolve`, {});
  }

  listBranches(): Observable<Branch[]> {
    return this.http.get<Branch[]>(`${this.apiBaseUrl}/operations/branches`);
  }

  createBranch(request: BranchUpsertRequest): Observable<Branch> {
    return this.http.post<Branch>(`${this.apiBaseUrl}/operations/branches`, request);
  }

  updateBranch(id: string, request: BranchUpsertRequest): Observable<Branch> {
    return this.http.put<Branch>(`${this.apiBaseUrl}/operations/branches/${id}`, request);
  }

  listCostCenters(branchId?: string | null): Observable<CostCenter[]> {
    return this.http.get<CostCenter[]>(`${this.apiBaseUrl}/operations/cost-centers`, {
      params: this.toParams({ branchId }),
    });
  }

  createCostCenter(request: CostCenterUpsertRequest): Observable<CostCenter> {
    return this.http.post<CostCenter>(`${this.apiBaseUrl}/operations/cost-centers`, request);
  }

  updateCostCenter(id: string, request: CostCenterUpsertRequest): Observable<CostCenter> {
    return this.http.put<CostCenter>(`${this.apiBaseUrl}/operations/cost-centers/${id}`, request);
  }

  listFeatureFlags(branchId?: string | null): Observable<FeatureFlag[]> {
    return this.http.get<FeatureFlag[]>(`${this.apiBaseUrl}/operations/feature-flags`, {
      params: this.toParams({ branchId }),
    });
  }

  updateFeatureFlag(id: string, request: FeatureFlagUpdateRequest): Observable<FeatureFlag> {
    return this.http.put<FeatureFlag>(`${this.apiBaseUrl}/operations/feature-flags/${id}`, request);
  }

  listApprovalRules(module?: number | null, branchId?: string | null): Observable<ApprovalRule[]> {
    return this.http.get<ApprovalRule[]>(`${this.apiBaseUrl}/operations/approval-rules`, {
      params: this.toParams({ module, branchId }),
    });
  }

  createApprovalRule(request: ApprovalRuleUpsertRequest): Observable<ApprovalRule> {
    return this.http.post<ApprovalRule>(`${this.apiBaseUrl}/operations/approval-rules`, request);
  }

  updateApprovalRule(id: string, request: ApprovalRuleUpsertRequest): Observable<ApprovalRule> {
    return this.http.put<ApprovalRule>(`${this.apiBaseUrl}/operations/approval-rules/${id}`, request);
  }

  listApprovalQueue(status?: number | null, module?: number | null, branchId?: string | null): Observable<ApprovalQueueItem[]> {
    return this.http.get<ApprovalQueueItem[]>(`${this.apiBaseUrl}/operations/approvals`, {
      params: this.toParams({ status, module, branchId }),
    });
  }

  listNotifications(branchId?: string | null, unreadOnly = false, limit = 30): Observable<NotificationItem[]> {
    return this.http.get<NotificationItem[]>(`${this.apiBaseUrl}/operations/notifications`, {
      params: this.toParams({ branchId, unreadOnly, limit }),
    });
  }

  markNotificationRead(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiBaseUrl}/operations/notifications/${id}/read`, {});
  }

  markAllNotificationsRead(branchId?: string | null): Observable<void> {
    return this.http.post<void>(`${this.apiBaseUrl}/operations/notifications/read-all`, {}, {
      params: this.toParams({ branchId }),
    });
  }

  private toParams(source: Record<string, unknown>): HttpParams {
    let params = new HttpParams();

    for (const [key, rawValue] of Object.entries(source)) {
      if (rawValue === undefined || rawValue === null || rawValue === '') {
        continue;
      }

      if (rawValue instanceof Date) {
        params = params.set(key, rawValue.toISOString());
        continue;
      }

      params = params.set(key, String(rawValue));
    }

    return params;
  }
}
