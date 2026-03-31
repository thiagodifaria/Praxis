START TRANSACTION;

ALTER TABLE sales_order_items ADD "LineCost" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE sales_order_items ADD "UnitCost" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE products ADD "StandardCost" numeric(18,2) NOT NULL DEFAULT 0.0;

CREATE TABLE invoices (
    "Id" uuid NOT NULL,
    "InvoiceNumber" character varying(50) NOT NULL,
    "SalesOrderId" uuid NOT NULL,
    "CustomerId" uuid NOT NULL,
    "Status" character varying(20) NOT NULL,
    "IssuedAtUtc" timestamp with time zone NOT NULL,
    "DueDateUtc" timestamp with time zone NOT NULL,
    "TotalAmount" numeric(18,2) NOT NULL,
    "Notes" character varying(500),
    "PaidAtUtc" timestamp with time zone,
    "CancelledAtUtc" timestamp with time zone,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_invoices" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_invoices_customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES customers ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_invoices_sales_orders_SalesOrderId" FOREIGN KEY ("SalesOrderId") REFERENCES sales_orders ("Id") ON DELETE RESTRICT
);

CREATE TABLE purchase_orders (
    "Id" uuid NOT NULL,
    "OrderNumber" character varying(40) NOT NULL,
    "Status" character varying(30) NOT NULL,
    "Notes" character varying(500),
    "TotalAmount" numeric(18,2) NOT NULL,
    "SupplierId" uuid NOT NULL,
    "WarehouseLocationId" uuid NOT NULL,
    "ExpectedDeliveryDateUtc" timestamp with time zone,
    "ApprovedAtUtc" timestamp with time zone,
    "ReceivedAtUtc" timestamp with time zone,
    "CancelledAtUtc" timestamp with time zone,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_purchase_orders" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_purchase_orders_suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES suppliers ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_purchase_orders_warehouse_locations_WarehouseLocationId" FOREIGN KEY ("WarehouseLocationId") REFERENCES warehouse_locations ("Id") ON DELETE RESTRICT
);

CREATE TABLE invoice_items (
    "Id" uuid NOT NULL,
    "InvoiceId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "Sku" character varying(50) NOT NULL,
    "ProductName" character varying(150) NOT NULL,
    "Quantity" integer NOT NULL,
    "UnitPrice" numeric(18,2) NOT NULL,
    "UnitCost" numeric(18,2) NOT NULL,
    "LineTotal" numeric(18,2) NOT NULL,
    "LineCost" numeric(18,2) NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_invoice_items" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_invoice_items_invoices_InvoiceId" FOREIGN KEY ("InvoiceId") REFERENCES invoices ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_invoice_items_products_ProductId" FOREIGN KEY ("ProductId") REFERENCES products ("Id") ON DELETE RESTRICT
);

CREATE TABLE receivables (
    "Id" uuid NOT NULL,
    "CustomerId" uuid NOT NULL,
    "InvoiceId" uuid NOT NULL,
    "DocumentNumber" character varying(50) NOT NULL,
    "Description" character varying(255) NOT NULL,
    "IssuedAtUtc" timestamp with time zone NOT NULL,
    "DueDateUtc" timestamp with time zone NOT NULL,
    "OriginalAmount" numeric(18,2) NOT NULL,
    "PaidAmount" numeric(18,2) NOT NULL,
    "OutstandingAmount" numeric(18,2) NOT NULL,
    "Status" character varying(20) NOT NULL,
    "SettledAtUtc" timestamp with time zone,
    "CancelledAtUtc" timestamp with time zone,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_receivables" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_receivables_customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES customers ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_receivables_invoices_InvoiceId" FOREIGN KEY ("InvoiceId") REFERENCES invoices ("Id") ON DELETE RESTRICT
);

CREATE TABLE purchase_order_items (
    "Id" uuid NOT NULL,
    "PurchaseOrderId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "Sku" character varying(50) NOT NULL,
    "ProductName" character varying(150) NOT NULL,
    "Quantity" integer NOT NULL,
    "ReceivedQuantity" integer NOT NULL,
    "UnitCost" numeric(18,2) NOT NULL,
    "LineTotal" numeric(18,2) NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_purchase_order_items" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_purchase_order_items_products_ProductId" FOREIGN KEY ("ProductId") REFERENCES products ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_purchase_order_items_purchase_orders_PurchaseOrderId" FOREIGN KEY ("PurchaseOrderId") REFERENCES purchase_orders ("Id") ON DELETE CASCADE
);

CREATE TABLE purchase_receipts (
    "Id" uuid NOT NULL,
    "PurchaseOrderId" uuid NOT NULL,
    "ReceiptNumber" character varying(50) NOT NULL,
    "TotalAmount" numeric(18,2) NOT NULL,
    "Notes" character varying(500),
    "ReceivedAtUtc" timestamp with time zone NOT NULL,
    "ReceivedByUserId" uuid,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_purchase_receipts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_purchase_receipts_purchase_orders_PurchaseOrderId" FOREIGN KEY ("PurchaseOrderId") REFERENCES purchase_orders ("Id") ON DELETE CASCADE
);

CREATE TABLE receivable_settlements (
    "Id" uuid NOT NULL,
    "ReceivableId" uuid NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "PaidAtUtc" timestamp with time zone NOT NULL,
    "PaymentMethod" character varying(80) NOT NULL,
    "Notes" character varying(255),
    "ReceivedByUserId" uuid,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_receivable_settlements" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_receivable_settlements_receivables_ReceivableId" FOREIGN KEY ("ReceivableId") REFERENCES receivables ("Id") ON DELETE CASCADE
);

CREATE TABLE payables (
    "Id" uuid NOT NULL,
    "SupplierId" uuid NOT NULL,
    "PurchaseOrderId" uuid,
    "PurchaseReceiptId" uuid,
    "DocumentNumber" character varying(50) NOT NULL,
    "Description" character varying(255) NOT NULL,
    "IssuedAtUtc" timestamp with time zone NOT NULL,
    "DueDateUtc" timestamp with time zone NOT NULL,
    "OriginalAmount" numeric(18,2) NOT NULL,
    "PaidAmount" numeric(18,2) NOT NULL,
    "OutstandingAmount" numeric(18,2) NOT NULL,
    "Status" character varying(20) NOT NULL,
    "SettledAtUtc" timestamp with time zone,
    "CancelledAtUtc" timestamp with time zone,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_payables" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_payables_purchase_orders_PurchaseOrderId" FOREIGN KEY ("PurchaseOrderId") REFERENCES purchase_orders ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_payables_purchase_receipts_PurchaseReceiptId" FOREIGN KEY ("PurchaseReceiptId") REFERENCES purchase_receipts ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_payables_suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES suppliers ("Id") ON DELETE RESTRICT
);

CREATE TABLE purchase_receipt_items (
    "Id" uuid NOT NULL,
    "PurchaseReceiptId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "Sku" character varying(50) NOT NULL,
    "ProductName" character varying(150) NOT NULL,
    "Quantity" integer NOT NULL,
    "UnitCost" numeric(18,2) NOT NULL,
    "LineTotal" numeric(18,2) NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_purchase_receipt_items" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_purchase_receipt_items_products_ProductId" FOREIGN KEY ("ProductId") REFERENCES products ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_purchase_receipt_items_purchase_receipts_PurchaseReceiptId" FOREIGN KEY ("PurchaseReceiptId") REFERENCES purchase_receipts ("Id") ON DELETE CASCADE
);

CREATE TABLE payable_settlements (
    "Id" uuid NOT NULL,
    "PayableId" uuid NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "PaidAtUtc" timestamp with time zone NOT NULL,
    "PaymentMethod" character varying(80) NOT NULL,
    "Notes" character varying(255),
    "PaidByUserId" uuid,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_payable_settlements" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_payable_settlements_payables_PayableId" FOREIGN KEY ("PayableId") REFERENCES payables ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_invoice_items_InvoiceId" ON invoice_items ("InvoiceId");

CREATE INDEX "IX_invoice_items_ProductId" ON invoice_items ("ProductId");

CREATE INDEX "IX_invoices_CustomerId" ON invoices ("CustomerId");

CREATE UNIQUE INDEX "IX_invoices_InvoiceNumber" ON invoices ("InvoiceNumber");

CREATE INDEX "IX_invoices_SalesOrderId_Status" ON invoices ("SalesOrderId", "Status");

CREATE INDEX "IX_payable_settlements_PayableId" ON payable_settlements ("PayableId");

CREATE UNIQUE INDEX "IX_payables_DocumentNumber" ON payables ("DocumentNumber");

CREATE INDEX "IX_payables_DueDateUtc_Status" ON payables ("DueDateUtc", "Status");

CREATE INDEX "IX_payables_PurchaseOrderId" ON payables ("PurchaseOrderId");

CREATE INDEX "IX_payables_PurchaseReceiptId" ON payables ("PurchaseReceiptId");

CREATE INDEX "IX_payables_SupplierId" ON payables ("SupplierId");

CREATE INDEX "IX_purchase_order_items_ProductId" ON purchase_order_items ("ProductId");

CREATE INDEX "IX_purchase_order_items_PurchaseOrderId" ON purchase_order_items ("PurchaseOrderId");

CREATE UNIQUE INDEX "IX_purchase_orders_OrderNumber" ON purchase_orders ("OrderNumber");

CREATE INDEX "IX_purchase_orders_SupplierId" ON purchase_orders ("SupplierId");

CREATE INDEX "IX_purchase_orders_WarehouseLocationId" ON purchase_orders ("WarehouseLocationId");

CREATE INDEX "IX_purchase_receipt_items_ProductId" ON purchase_receipt_items ("ProductId");

CREATE INDEX "IX_purchase_receipt_items_PurchaseReceiptId" ON purchase_receipt_items ("PurchaseReceiptId");

CREATE INDEX "IX_purchase_receipts_PurchaseOrderId" ON purchase_receipts ("PurchaseOrderId");

CREATE UNIQUE INDEX "IX_purchase_receipts_ReceiptNumber" ON purchase_receipts ("ReceiptNumber");

CREATE INDEX "IX_receivable_settlements_ReceivableId" ON receivable_settlements ("ReceivableId");

CREATE INDEX "IX_receivables_CustomerId" ON receivables ("CustomerId");

CREATE UNIQUE INDEX "IX_receivables_DocumentNumber" ON receivables ("DocumentNumber");

CREATE INDEX "IX_receivables_DueDateUtc_Status" ON receivables ("DueDateUtc", "Status");

CREATE INDEX "IX_receivables_InvoiceId" ON receivables ("InvoiceId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260330173950_AddV2Modules', '8.0.4');

COMMIT;

