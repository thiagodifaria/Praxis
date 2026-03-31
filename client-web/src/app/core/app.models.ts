export type StatusValue = string | number;

export interface AuthUser {
  id: string;
  fullName: string;
  email: string;
  role: string;
  permissions: string[];
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  user: AuthUser;
}

export interface Branch {
  id: string;
  code: string;
  name: string;
  legalName: string;
  document: string;
  city: string;
  state: string;
  isHeadquarters: boolean;
  isActive: boolean;
}

export interface BranchUpsertRequest {
  code: string;
  name: string;
  legalName: string;
  document: string;
  city: string;
  state: string;
  isHeadquarters: boolean;
  isActive: boolean;
}

export interface CostCenter {
  id: string;
  branchId: string;
  branchName: string;
  code: string;
  name: string;
  description: string;
  isActive: boolean;
}

export interface CostCenterUpsertRequest {
  branchId: string;
  code: string;
  name: string;
  description: string;
  isActive: boolean;
}

export interface FeatureFlag {
  id: string;
  moduleKey: string;
  displayName: string;
  description: string;
  isEnabled: boolean;
  branchId?: string | null;
  branchName?: string | null;
}

export interface FeatureFlagUpdateRequest {
  displayName: string;
  description: string;
  isEnabled: boolean;
}

export interface ApprovalRule {
  id: string;
  name: string;
  module: StatusValue;
  branchId?: string | null;
  branchName?: string | null;
  minimumAmount: number;
  requiredRoleName: string;
  description: string;
  isActive: boolean;
}

export interface ApprovalRuleUpsertRequest {
  name: string;
  module: number;
  branchId?: string | null;
  minimumAmount: number;
  requiredRoleName: string;
  description: string;
  isActive: boolean;
}

export interface ApprovalQueueItem {
  id: string;
  module: StatusValue;
  status: StatusValue;
  entityId: string;
  referenceNumber: string;
  requestedAmount: number;
  approvalRuleId: string;
  approvalRuleName: string;
  requiredRoleName: string;
  branchId?: string | null;
  branchName?: string | null;
  costCenterId?: string | null;
  costCenterName?: string | null;
  requestedAtUtc: string;
  decidedAtUtc?: string | null;
  requestedByName?: string | null;
  decisionByName?: string | null;
  notes?: string | null;
}

export interface NotificationItem {
  id: string;
  routingKey: string;
  source: string;
  title: string;
  message: string;
  severity: StatusValue;
  isRead: boolean;
  publishedAtUtc: string;
  branchId?: string | null;
  branchName?: string | null;
  metadataJson: string;
}

export interface LiveNotificationMessage {
  routingKey: string;
  source: string;
  title: string;
  message: string;
  severity: StatusValue;
  branchId?: string | null;
  metadataJson: string;
  publishedAtUtc: string;
}

export interface Category {
  id: string;
  code: string;
  name: string;
  description: string;
  isActive: boolean;
}

export interface CategoryUpsertRequest {
  code: string;
  name: string;
  description: string;
  isActive: boolean;
}

export interface Supplier {
  id: string;
  code: string;
  name: string;
  contactName?: string | null;
  email?: string | null;
  phone?: string | null;
  isActive: boolean;
}

export interface SupplierUpsertRequest {
  code: string;
  name: string;
  contactName?: string | null;
  email?: string | null;
  phone?: string | null;
  isActive: boolean;
}

export interface Product {
  id: string;
  sku: string;
  name: string;
  description: string;
  unitPrice: number;
  standardCost: number;
  reorderLevel: number;
  isActive: boolean;
  categoryId: string;
  categoryName: string;
  supplierId?: string | null;
  supplierName?: string | null;
}

export interface ProductUpsertRequest {
  sku: string;
  name: string;
  description: string;
  unitPrice: number;
  standardCost: number;
  reorderLevel: number;
  categoryId: string;
  supplierId?: string | null;
  isActive: boolean;
}

export interface Customer {
  id: string;
  code: string;
  name: string;
  document: string;
  email?: string | null;
  phone?: string | null;
  status: StatusValue;
}

export interface CustomerUpsertRequest {
  code: string;
  name: string;
  document: string;
  email?: string | null;
  phone?: string | null;
  status: number;
}

export interface Warehouse {
  id: string;
  branchId?: string | null;
  branchName?: string | null;
  code: string;
  name: string;
  description: string;
  isDefault: boolean;
}

export interface SalesOrderItemRequest {
  productId: string;
  quantity: number;
}

export interface SalesOrderItem {
  productId: string;
  sku: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface CreateSalesOrderRequest {
  customerId: string;
  branchId: string;
  costCenterId?: string | null;
  warehouseLocationId: string;
  notes?: string | null;
  items: SalesOrderItemRequest[];
}

export interface SalesOrder {
  id: string;
  orderNumber: string;
  customerId: string;
  customerName: string;
  branchId?: string | null;
  branchName?: string | null;
  costCenterId?: string | null;
  costCenterName?: string | null;
  warehouseLocationId: string;
  warehouseName: string;
  status: StatusValue;
  approvalStatus: StatusValue;
  totalAmount: number;
  notes?: string | null;
  createdAtUtc: string;
  items: SalesOrderItem[];
}

export interface PurchaseOrderItemRequest {
  productId: string;
  quantity: number;
  unitCost: number;
}

export interface PurchaseOrderItem {
  productId: string;
  sku: string;
  productName: string;
  quantity: number;
  receivedQuantity: number;
  unitCost: number;
  lineTotal: number;
}

export interface CreatePurchaseOrderRequest {
  supplierId: string;
  branchId: string;
  costCenterId?: string | null;
  warehouseLocationId: string;
  expectedDeliveryDateUtc?: string | null;
  notes?: string | null;
  items: PurchaseOrderItemRequest[];
}

export interface ReceivePurchaseOrderRequest {
  receivedAtUtc: string;
  dueDateUtc: string;
  notes?: string | null;
  items: Array<{
    productId: string;
    quantity: number;
  }>;
}

export interface PurchaseReceipt {
  id: string;
  receiptNumber: string;
  totalAmount: number;
  notes?: string | null;
  receivedAtUtc: string;
  items: Array<{
    productId: string;
    sku: string;
    productName: string;
    quantity: number;
    unitCost: number;
    lineTotal: number;
  }>;
}

export interface PurchaseOrder {
  id: string;
  orderNumber: string;
  supplierId: string;
  supplierName: string;
  branchId?: string | null;
  branchName?: string | null;
  costCenterId?: string | null;
  costCenterName?: string | null;
  warehouseLocationId: string;
  warehouseName: string;
  status: StatusValue;
  approvalStatus: StatusValue;
  totalAmount: number;
  expectedDeliveryDateUtc?: string | null;
  notes?: string | null;
  createdAtUtc: string;
  items: PurchaseOrderItem[];
  receipts: PurchaseReceipt[];
}

export interface OrderStatusRequest {
  notes?: string | null;
}

export interface Invoice {
  id: string;
  invoiceNumber: string;
  salesOrderId: string;
  salesOrderNumber: string;
  customerId: string;
  customerName: string;
  branchId?: string | null;
  branchName?: string | null;
  costCenterId?: string | null;
  costCenterName?: string | null;
  status: StatusValue;
  issuedAtUtc: string;
  dueDateUtc: string;
  totalAmount: number;
  notes?: string | null;
  items: Array<{
    productId: string;
    sku: string;
    productName: string;
    quantity: number;
    unitPrice: number;
    unitCost: number;
    lineTotal: number;
    lineCost: number;
  }>;
}

export interface IssueInvoiceRequest {
  salesOrderId: string;
  dueDateUtc: string;
  notes?: string | null;
}

export interface Settlement {
  id: string;
  amount: number;
  paidAtUtc: string;
  paymentMethod: string;
  notes?: string | null;
}

export interface Receivable {
  id: string;
  documentNumber: string;
  invoiceId: string;
  invoiceNumber: string;
  customerId: string;
  customerName: string;
  branchId?: string | null;
  branchName?: string | null;
  costCenterId?: string | null;
  costCenterName?: string | null;
  description: string;
  issuedAtUtc: string;
  dueDateUtc: string;
  originalAmount: number;
  paidAmount: number;
  outstandingAmount: number;
  status: StatusValue;
  settlements: Settlement[];
}

export interface Payable {
  id: string;
  documentNumber: string;
  purchaseOrderId?: string | null;
  purchaseOrderNumber?: string | null;
  purchaseReceiptId?: string | null;
  purchaseReceiptNumber?: string | null;
  supplierId: string;
  supplierName: string;
  branchId?: string | null;
  branchName?: string | null;
  costCenterId?: string | null;
  costCenterName?: string | null;
  description: string;
  issuedAtUtc: string;
  dueDateUtc: string;
  originalAmount: number;
  paidAmount: number;
  outstandingAmount: number;
  status: StatusValue;
  settlements: Settlement[];
}

export interface FinancialSettlementRequest {
  amount: number;
  paidAtUtc: string;
  paymentMethod: string;
  notes?: string | null;
}

export interface InventoryBalance {
  productId: string;
  productName: string;
  productSku: string;
  branchId?: string | null;
  branchName?: string | null;
  warehouseLocationId: string;
  warehouseName: string;
  onHandQuantity: number;
  reservedQuantity: number;
  availableQuantity: number;
  reorderLevel: number;
}

export interface StockMovement {
  id: string;
  productId: string;
  productName: string;
  productSku: string;
  warehouseLocationId: string;
  warehouseName: string;
  type: StatusValue;
  quantity: number;
  reason: string;
  referenceType: string;
  referenceId?: string | null;
  createdAtUtc: string;
}

export interface AdjustInventoryRequest {
  productId: string;
  warehouseLocationId: string;
  quantityDelta: number;
  reason: string;
}

export interface DashboardSnapshot {
  activeCustomers: number;
  activeProducts: number;
  draftOrders: number;
  approvedOrders: number;
  approvedPurchaseOrders: number;
  issuedInvoices: number;
  lowStockProducts: number;
  pendingApprovals: number;
  unreadNotifications: number;
  orderPipelineAmount: number;
  openReceivablesAmount: number;
  overdueReceivablesAmount: number;
  openPayablesAmount: number;
  recentOrders: Array<{ orderNumber: string; customerName: string; status: StatusValue; totalAmount: number; createdAtUtc: string }>;
  recentInvoices: Array<{ invoiceNumber: string; customerName: string; status: StatusValue; totalAmount: number; issuedAtUtc: string }>;
  lowStockItems: Array<{ productId: string; productName: string; productSku: string; availableQuantity: number; reorderLevel: number; warehouseName: string }>;
  recentMovements: Array<{ productName: string; productSku: string; warehouseName: string; reason: string; quantity: number; createdAtUtc: string }>;
  openAlerts: Array<{ id: string; code: string; title: string; severity: StatusValue; status: StatusValue; createdAtUtc: string }>;
  branches: Array<{ branchId?: string | null; branchName: string; activeOrders: number; activePurchaseOrders: number; openReceivablesAmount: number; openAlerts: number }>;
  generatedAtUtc: string;
}

export interface ReportingOverview {
  generatedAtUtc: string;
  fromUtc: string;
  toUtc: string;
  grossRevenue: number;
  grossCost: number;
  grossMargin: number;
  grossMarginPercentage: number;
  issuedInvoices: number;
  dispatchedOrders: number;
  receivedPurchaseOrders: number;
  openReceivablesAmount: number;
  overdueReceivablesAmount: number;
  openPayablesAmount: number;
  topCustomers: Array<{ customerId: string; customerName: string; revenue: number; margin: number }>;
  topSuppliers: Array<{ supplierId: string; supplierName: string; spend: number }>;
  branchPerformance: Array<{ branchId?: string | null; branchName: string; revenue: number; openReceivablesAmount: number; openAlerts: number }>;
  costCenterPerformance: Array<{ costCenterId?: string | null; costCenterName: string; revenue: number; spend: number }>;
}

export interface InventoryTurnoverReport {
  generatedAtUtc: string;
  fromUtc: string;
  toUtc: string;
  inventoryValue: number;
  inboundQuantity: number;
  outboundQuantity: number;
  stockTurnoverRatio: number;
  items: Array<{ productId: string; productName: string; productSku: string; onHandQuantity: number; outboundQuantity: number; turnoverRatio: number }>;
}

export interface OverdueReceivablesReport {
  generatedAtUtc: string;
  totalOpenTitles: number;
  overdueTitles: number;
  overdueAmount: number;
  items: Array<{ receivableId: string; documentNumber: string; customerName: string; dueDateUtc: string; daysOverdue: number; outstandingAmount: number }>;
}

export interface AuditEntry {
  id: string;
  eventType: string;
  entityName: string;
  entityId: string;
  actorUserId?: string | null;
  actorName?: string | null;
  metadataJson: string;
  createdAtUtc: string;
}

export interface OperationalAlert {
  id: string;
  code: string;
  title: string;
  message: string;
  source: string;
  branchId?: string | null;
  branchName?: string | null;
  referenceId?: string | null;
  severity: StatusValue;
  status: StatusValue;
  createdAtUtc: string;
  resolvedAtUtc?: string | null;
}
