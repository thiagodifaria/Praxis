START TRANSACTION;

ALTER TABLE warehouse_locations ADD "BranchId" uuid;

ALTER TABLE sales_orders ADD "ApprovalDecidedAtUtc" timestamp with time zone;

ALTER TABLE sales_orders ADD "ApprovalRequestedAtUtc" timestamp with time zone;

ALTER TABLE sales_orders ADD "ApprovalRuleId" uuid;

ALTER TABLE sales_orders ADD "ApprovalStatus" character varying(20) NOT NULL DEFAULT '';

ALTER TABLE sales_orders ADD "ApprovedByName" text;

ALTER TABLE sales_orders ADD "ApprovedByUserId" uuid;

ALTER TABLE sales_orders ADD "BranchId" uuid;

ALTER TABLE sales_orders ADD "CostCenterId" uuid;

ALTER TABLE receivables ADD "BranchId" uuid;

ALTER TABLE receivables ADD "CostCenterId" uuid;

ALTER TABLE purchase_orders ADD "ApprovalDecidedAtUtc" timestamp with time zone;

ALTER TABLE purchase_orders ADD "ApprovalRequestedAtUtc" timestamp with time zone;

ALTER TABLE purchase_orders ADD "ApprovalRuleId" uuid;

ALTER TABLE purchase_orders ADD "ApprovalStatus" character varying(20) NOT NULL DEFAULT '';

ALTER TABLE purchase_orders ADD "ApprovedByName" text;

ALTER TABLE purchase_orders ADD "ApprovedByUserId" uuid;

ALTER TABLE purchase_orders ADD "BranchId" uuid;

ALTER TABLE purchase_orders ADD "CostCenterId" uuid;

ALTER TABLE payables ADD "BranchId" uuid;

ALTER TABLE payables ADD "CostCenterId" uuid;

ALTER TABLE operational_alerts ADD "BranchId" uuid;

ALTER TABLE invoices ADD "BranchId" uuid;

ALTER TABLE invoices ADD "CostCenterId" uuid;

CREATE TABLE branches (
    "Id" uuid NOT NULL,
    "Code" character varying(20) NOT NULL,
    "Name" character varying(120) NOT NULL,
    "LegalName" character varying(180) NOT NULL,
    "Document" character varying(40) NOT NULL,
    "City" character varying(120) NOT NULL,
    "State" character varying(20) NOT NULL,
    "IsHeadquarters" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_branches" PRIMARY KEY ("Id")
);

CREATE TABLE approval_rules (
    "Id" uuid NOT NULL,
    "Name" character varying(120) NOT NULL,
    "Module" character varying(40) NOT NULL,
    "BranchId" uuid,
    "MinimumAmount" numeric(18,2) NOT NULL,
    "RequiredRoleName" character varying(80) NOT NULL,
    "Description" character varying(255) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_approval_rules" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_approval_rules_branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES branches ("Id") ON DELETE RESTRICT
);

CREATE TABLE cost_centers (
    "Id" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "Code" character varying(30) NOT NULL,
    "Name" character varying(120) NOT NULL,
    "Description" character varying(255) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_cost_centers" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_cost_centers_branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES branches ("Id") ON DELETE RESTRICT
);

CREATE TABLE module_feature_flags (
    "Id" uuid NOT NULL,
    "ModuleKey" character varying(50) NOT NULL,
    "DisplayName" character varying(120) NOT NULL,
    "Description" character varying(255) NOT NULL,
    "BranchId" uuid,
    "IsEnabled" boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_module_feature_flags" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_module_feature_flags_branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES branches ("Id") ON DELETE RESTRICT
);

CREATE TABLE realtime_notifications (
    "Id" uuid NOT NULL,
    "RoutingKey" character varying(120) NOT NULL,
    "Source" character varying(80) NOT NULL,
    "Title" character varying(180) NOT NULL,
    "Message" character varying(500) NOT NULL,
    "Severity" character varying(20) NOT NULL,
    "BranchId" uuid,
    "RecipientUserId" uuid,
    "IsRead" boolean NOT NULL,
    "PublishedAtUtc" timestamp with time zone NOT NULL,
    "ReadAtUtc" timestamp with time zone,
    "MetadataJson" jsonb NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_realtime_notifications" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_realtime_notifications_branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES branches ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_realtime_notifications_users_RecipientUserId" FOREIGN KEY ("RecipientUserId") REFERENCES users ("Id") ON DELETE RESTRICT
);

CREATE TABLE approval_decisions (
    "Id" uuid NOT NULL,
    "Module" character varying(40) NOT NULL,
    "EntityId" uuid NOT NULL,
    "ApprovalRuleId" uuid NOT NULL,
    "Status" character varying(30) NOT NULL,
    "ReferenceNumber" character varying(50) NOT NULL,
    "RequestedAmount" numeric(18,2) NOT NULL,
    "BranchId" uuid,
    "CostCenterId" uuid,
    "RequestedByUserId" uuid,
    "RequestedByName" character varying(150),
    "DecisionByUserId" uuid,
    "DecisionByName" character varying(150),
    "Notes" character varying(500),
    "RequestedAtUtc" timestamp with time zone NOT NULL,
    "DecidedAtUtc" timestamp with time zone,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_approval_decisions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_approval_decisions_approval_rules_ApprovalRuleId" FOREIGN KEY ("ApprovalRuleId") REFERENCES approval_rules ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_approval_decisions_branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES branches ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_approval_decisions_cost_centers_CostCenterId" FOREIGN KEY ("CostCenterId") REFERENCES cost_centers ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_warehouse_locations_BranchId" ON warehouse_locations ("BranchId");

CREATE INDEX "IX_sales_orders_ApprovalRuleId" ON sales_orders ("ApprovalRuleId");

CREATE INDEX "IX_sales_orders_BranchId" ON sales_orders ("BranchId");

CREATE INDEX "IX_sales_orders_CostCenterId" ON sales_orders ("CostCenterId");

CREATE INDEX "IX_receivables_BranchId" ON receivables ("BranchId");

CREATE INDEX "IX_receivables_CostCenterId" ON receivables ("CostCenterId");

CREATE INDEX "IX_purchase_orders_ApprovalRuleId" ON purchase_orders ("ApprovalRuleId");

CREATE INDEX "IX_purchase_orders_BranchId" ON purchase_orders ("BranchId");

CREATE INDEX "IX_purchase_orders_CostCenterId" ON purchase_orders ("CostCenterId");

CREATE INDEX "IX_payables_BranchId" ON payables ("BranchId");

CREATE INDEX "IX_payables_CostCenterId" ON payables ("CostCenterId");

CREATE INDEX "IX_operational_alerts_BranchId" ON operational_alerts ("BranchId");

CREATE INDEX "IX_invoices_BranchId" ON invoices ("BranchId");

CREATE INDEX "IX_invoices_CostCenterId" ON invoices ("CostCenterId");

CREATE INDEX "IX_approval_decisions_ApprovalRuleId" ON approval_decisions ("ApprovalRuleId");

CREATE INDEX "IX_approval_decisions_BranchId" ON approval_decisions ("BranchId");

CREATE INDEX "IX_approval_decisions_CostCenterId" ON approval_decisions ("CostCenterId");

CREATE INDEX "IX_approval_decisions_Module_EntityId_Status" ON approval_decisions ("Module", "EntityId", "Status");

CREATE INDEX "IX_approval_rules_BranchId" ON approval_rules ("BranchId");

CREATE INDEX "IX_approval_rules_Module_BranchId_MinimumAmount" ON approval_rules ("Module", "BranchId", "MinimumAmount");

CREATE UNIQUE INDEX "IX_branches_Code" ON branches ("Code");

CREATE UNIQUE INDEX "IX_cost_centers_BranchId_Code" ON cost_centers ("BranchId", "Code");

CREATE INDEX "IX_module_feature_flags_BranchId" ON module_feature_flags ("BranchId");

CREATE UNIQUE INDEX "IX_module_feature_flags_ModuleKey_BranchId" ON module_feature_flags ("ModuleKey", "BranchId");

CREATE INDEX "IX_realtime_notifications_BranchId" ON realtime_notifications ("BranchId");

CREATE INDEX "IX_realtime_notifications_IsRead_PublishedAtUtc" ON realtime_notifications ("IsRead", "PublishedAtUtc");

CREATE INDEX "IX_realtime_notifications_RecipientUserId" ON realtime_notifications ("RecipientUserId");

ALTER TABLE invoices ADD CONSTRAINT "FK_invoices_branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES branches ("Id") ON DELETE RESTRICT;

ALTER TABLE invoices ADD CONSTRAINT "FK_invoices_cost_centers_CostCenterId" FOREIGN KEY ("CostCenterId") REFERENCES cost_centers ("Id") ON DELETE RESTRICT;

ALTER TABLE operational_alerts ADD CONSTRAINT "FK_operational_alerts_branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES branches ("Id") ON DELETE RESTRICT;

ALTER TABLE payables ADD CONSTRAINT "FK_payables_branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES branches ("Id") ON DELETE RESTRICT;

ALTER TABLE payables ADD CONSTRAINT "FK_payables_cost_centers_CostCenterId" FOREIGN KEY ("CostCenterId") REFERENCES cost_centers ("Id") ON DELETE RESTRICT;

ALTER TABLE purchase_orders ADD CONSTRAINT "FK_purchase_orders_approval_rules_ApprovalRuleId" FOREIGN KEY ("ApprovalRuleId") REFERENCES approval_rules ("Id") ON DELETE RESTRICT;

ALTER TABLE purchase_orders ADD CONSTRAINT "FK_purchase_orders_branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES branches ("Id") ON DELETE RESTRICT;

ALTER TABLE purchase_orders ADD CONSTRAINT "FK_purchase_orders_cost_centers_CostCenterId" FOREIGN KEY ("CostCenterId") REFERENCES cost_centers ("Id") ON DELETE RESTRICT;

ALTER TABLE receivables ADD CONSTRAINT "FK_receivables_branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES branches ("Id") ON DELETE RESTRICT;

ALTER TABLE receivables ADD CONSTRAINT "FK_receivables_cost_centers_CostCenterId" FOREIGN KEY ("CostCenterId") REFERENCES cost_centers ("Id") ON DELETE RESTRICT;

ALTER TABLE sales_orders ADD CONSTRAINT "FK_sales_orders_approval_rules_ApprovalRuleId" FOREIGN KEY ("ApprovalRuleId") REFERENCES approval_rules ("Id") ON DELETE RESTRICT;

ALTER TABLE sales_orders ADD CONSTRAINT "FK_sales_orders_branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES branches ("Id") ON DELETE RESTRICT;

ALTER TABLE sales_orders ADD CONSTRAINT "FK_sales_orders_cost_centers_CostCenterId" FOREIGN KEY ("CostCenterId") REFERENCES cost_centers ("Id") ON DELETE RESTRICT;

ALTER TABLE warehouse_locations ADD CONSTRAINT "FK_warehouse_locations_branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES branches ("Id") ON DELETE RESTRICT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260330212212_AddV3OperationalModules', '8.0.4');

COMMIT;

