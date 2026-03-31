CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE TABLE audit_entries (
        "Id" uuid NOT NULL,
        "EventType" character varying(100) NOT NULL,
        "EntityName" character varying(100) NOT NULL,
        "EntityId" character varying(120) NOT NULL,
        "ActorUserId" uuid,
        "ActorName" character varying(150),
        "MetadataJson" jsonb NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_audit_entries" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE TABLE categories (
        "Id" uuid NOT NULL,
        "Code" character varying(30) NOT NULL,
        "Name" character varying(150) NOT NULL,
        "Description" character varying(400) NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_categories" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE TABLE customers (
        "Id" uuid NOT NULL,
        "Code" character varying(30) NOT NULL,
        "Name" character varying(150) NOT NULL,
        "Document" character varying(40) NOT NULL,
        "Email" character varying(150),
        "Phone" character varying(50),
        "Status" character varying(20) NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_customers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE TABLE operational_alerts (
        "Id" uuid NOT NULL,
        "Code" character varying(120) NOT NULL,
        "Title" character varying(180) NOT NULL,
        "Message" character varying(500) NOT NULL,
        "Source" character varying(120) NOT NULL,
        "ReferenceId" character varying(120),
        "Severity" character varying(20) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "ResolvedAtUtc" timestamp with time zone,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_operational_alerts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE TABLE permissions (
        "Id" uuid NOT NULL,
        "Code" character varying(100) NOT NULL,
        "Description" character varying(255) NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_permissions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE TABLE roles (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Description" character varying(255) NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_roles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE TABLE stock_movements (
        "Id" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "WarehouseLocationId" uuid NOT NULL,
        "Type" character varying(30) NOT NULL,
        "Quantity" integer NOT NULL,
        "Reason" character varying(255) NOT NULL,
        "ReferenceType" character varying(100) NOT NULL,
        "ReferenceId" uuid,
        "CreatedByUserId" uuid,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_stock_movements" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE TABLE stock_reservations (
        "Id" uuid NOT NULL,
        "SalesOrderId" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "WarehouseLocationId" uuid NOT NULL,
        "Quantity" integer NOT NULL,
        "Status" character varying(20) NOT NULL,
        "ReleasedAtUtc" timestamp with time zone,
        "ConsumedAtUtc" timestamp with time zone,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_stock_reservations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE TABLE suppliers (
        "Id" uuid NOT NULL,
        "Code" character varying(30) NOT NULL,
        "Name" character varying(150) NOT NULL,
        "ContactName" character varying(150),
        "Email" character varying(150),
        "Phone" character varying(50),
        "IsActive" boolean NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_suppliers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE TABLE warehouse_locations (
        "Id" uuid NOT NULL,
        "Code" character varying(30) NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Description" character varying(255) NOT NULL,
        "IsDefault" boolean NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_warehouse_locations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE TABLE sales_orders (
        "Id" uuid NOT NULL,
        "OrderNumber" character varying(40) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "Notes" character varying(500),
        "TotalAmount" numeric(18,2) NOT NULL,
        "CustomerId" uuid NOT NULL,
        "WarehouseLocationId" uuid NOT NULL,
        "ApprovedAtUtc" timestamp with time zone,
        "DispatchedAtUtc" timestamp with time zone,
        "CancelledAtUtc" timestamp with time zone,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_sales_orders" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_sales_orders_customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES customers ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE TABLE role_permissions (
        "RoleId" uuid NOT NULL,
        "PermissionId" uuid NOT NULL,
        CONSTRAINT "PK_role_permissions" PRIMARY KEY ("RoleId", "PermissionId"),
        CONSTRAINT "FK_role_permissions_permissions_PermissionId" FOREIGN KEY ("PermissionId") REFERENCES permissions ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_role_permissions_roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES roles ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE TABLE users (
        "Id" uuid NOT NULL,
        "FullName" character varying(255) NOT NULL,
        "Email" character varying(255) NOT NULL,
        "PasswordHash" character varying(255) NOT NULL,
        "IsActive" boolean NOT NULL,
        "LastLoginAtUtc" timestamp with time zone,
        "RoleId" uuid NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_users" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_users_roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES roles ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE TABLE products (
        "Id" uuid NOT NULL,
        "Sku" character varying(50) NOT NULL,
        "Name" character varying(150) NOT NULL,
        "Description" character varying(500) NOT NULL,
        "UnitPrice" numeric(18,2) NOT NULL,
        "ReorderLevel" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CategoryId" uuid NOT NULL,
        "SupplierId" uuid,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_products" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_products_categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES categories ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_products_suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES suppliers ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE TABLE inventory_balances (
        "Id" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "WarehouseLocationId" uuid NOT NULL,
        "OnHandQuantity" integer NOT NULL,
        "ReservedQuantity" integer NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_inventory_balances" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_inventory_balances_warehouse_locations_WarehouseLocationId" FOREIGN KEY ("WarehouseLocationId") REFERENCES warehouse_locations ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE TABLE sales_order_items (
        "Id" uuid NOT NULL,
        "SalesOrderId" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "Sku" character varying(50) NOT NULL,
        "ProductName" character varying(150) NOT NULL,
        "Quantity" integer NOT NULL,
        "UnitPrice" numeric(18,2) NOT NULL,
        "LineTotal" numeric(18,2) NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_sales_order_items" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_sales_order_items_sales_orders_SalesOrderId" FOREIGN KEY ("SalesOrderId") REFERENCES sales_orders ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE TABLE refresh_tokens (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Token" character varying(255) NOT NULL,
        "ExpiresAtUtc" timestamp with time zone NOT NULL,
        "CreatedByIp" character varying(100),
        "RevokedAtUtc" timestamp with time zone,
        "RevokedByIp" character varying(100),
        "ReplacedByToken" character varying(255),
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_refresh_tokens" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_refresh_tokens_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_categories_Code" ON categories ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_customers_Code" ON customers ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_customers_Document" ON customers ("Document");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_inventory_balances_ProductId_WarehouseLocationId" ON inventory_balances ("ProductId", "WarehouseLocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE INDEX "IX_inventory_balances_WarehouseLocationId" ON inventory_balances ("WarehouseLocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE INDEX "IX_operational_alerts_Code_Status" ON operational_alerts ("Code", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_permissions_Code" ON permissions ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE INDEX "IX_products_CategoryId" ON products ("CategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_products_Sku" ON products ("Sku");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE INDEX "IX_products_SupplierId" ON products ("SupplierId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_refresh_tokens_Token" ON refresh_tokens ("Token");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE INDEX "IX_refresh_tokens_UserId" ON refresh_tokens ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE INDEX "IX_role_permissions_PermissionId" ON role_permissions ("PermissionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_roles_Name" ON roles ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE INDEX "IX_sales_order_items_SalesOrderId" ON sales_order_items ("SalesOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE INDEX "IX_sales_orders_CustomerId" ON sales_orders ("CustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_sales_orders_OrderNumber" ON sales_orders ("OrderNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE INDEX "IX_stock_reservations_SalesOrderId_ProductId_WarehouseLocation~" ON stock_reservations ("SalesOrderId", "ProductId", "WarehouseLocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_suppliers_Code" ON suppliers ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_users_Email" ON users ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE INDEX "IX_users_RoleId" ON users ("RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_warehouse_locations_Code" ON warehouse_locations ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329224038_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260329224038_InitialCreate', '8.0.4');
    END IF;
END $EF$;
COMMIT;

