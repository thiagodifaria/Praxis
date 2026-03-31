using Microsoft.EntityFrameworkCore;
using Praxis.Application.Persistence;
using Praxis.Domain.Billing;
using Praxis.Domain.Catalog;
using Praxis.Domain.Common;
using Praxis.Domain.Identity;
using Praxis.Domain.Inventory;
using Praxis.Domain.Operations;
using Praxis.Domain.Purchasing;
using Praxis.Domain.Sales;

namespace Praxis.Infrastructure.Persistence;

public sealed class PraxisDbContext(DbContextOptions<PraxisDbContext> options) : DbContext(options), IPraxisDbContext
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<PurchaseReceipt> PurchaseReceipts => Set<PurchaseReceipt>();
    public DbSet<PurchaseReceiptItem> PurchaseReceiptItems => Set<PurchaseReceiptItem>();
    public DbSet<WarehouseLocation> WarehouseLocations => Set<WarehouseLocation>();
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Receivable> Receivables => Set<Receivable>();
    public DbSet<ReceivableSettlement> ReceivableSettlements => Set<ReceivableSettlement>();
    public DbSet<Payable> Payables => Set<Payable>();
    public DbSet<PayableSettlement> PayableSettlements => Set<PayableSettlement>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<OperationalAlert> OperationalAlerts => Set<OperationalAlert>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<ModuleFeatureFlag> ModuleFeatureFlags => Set<ModuleFeatureFlag>();
    public DbSet<ApprovalRule> ApprovalRules => Set<ApprovalRule>();
    public DbSet<ApprovalDecision> ApprovalDecisions => Set<ApprovalDecision>();
    public DbSet<RealtimeNotification> RealtimeNotifications => Set<RealtimeNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(builder =>
        {
            builder.ToTable("roles");
            builder.HasKey(role => role.Id);
            builder.Property(role => role.Name).HasMaxLength(100).IsRequired();
            builder.Property(role => role.Description).HasMaxLength(255).IsRequired();
            builder.HasIndex(role => role.Name).IsUnique();
        });

        modelBuilder.Entity<Permission>(builder =>
        {
            builder.ToTable("permissions");
            builder.HasKey(permission => permission.Id);
            builder.Property(permission => permission.Code).HasMaxLength(100).IsRequired();
            builder.Property(permission => permission.Description).HasMaxLength(255).IsRequired();
            builder.HasIndex(permission => permission.Code).IsUnique();
        });

        modelBuilder.Entity<RolePermission>(builder =>
        {
            builder.ToTable("role_permissions");
            builder.HasKey(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId });
            builder.HasOne(rolePermission => rolePermission.Role)
                .WithMany(role => role.RolePermissions)
                .HasForeignKey(rolePermission => rolePermission.RoleId);
            builder.HasOne(rolePermission => rolePermission.Permission)
                .WithMany(permission => permission.RolePermissions)
                .HasForeignKey(rolePermission => rolePermission.PermissionId);
        });

        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(user => user.Id);
            builder.Property(user => user.FullName).HasMaxLength(255).IsRequired();
            builder.Property(user => user.Email).HasMaxLength(255).IsRequired();
            builder.Property(user => user.PasswordHash).HasMaxLength(255).IsRequired();
            builder.HasIndex(user => user.Email).IsUnique();
            builder.HasOne(user => user.Role)
                .WithMany(role => role.Users)
                .HasForeignKey(user => user.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(builder =>
        {
            builder.ToTable("refresh_tokens");
            builder.HasKey(token => token.Id);
            builder.Property(token => token.Token).HasMaxLength(255).IsRequired();
            builder.Property(token => token.CreatedByIp).HasMaxLength(100);
            builder.Property(token => token.RevokedByIp).HasMaxLength(100);
            builder.Property(token => token.ReplacedByToken).HasMaxLength(255);
            builder.HasIndex(token => token.Token).IsUnique();
            builder.Ignore(token => token.IsActive);
            builder.HasOne(token => token.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(token => token.UserId);
        });

        modelBuilder.Entity<Category>(builder =>
        {
            builder.ToTable("categories");
            builder.HasKey(category => category.Id);
            builder.Property(category => category.Code).HasMaxLength(30).IsRequired();
            builder.Property(category => category.Name).HasMaxLength(150).IsRequired();
            builder.Property(category => category.Description).HasMaxLength(400).IsRequired();
            builder.HasIndex(category => category.Code).IsUnique();
        });

        modelBuilder.Entity<Supplier>(builder =>
        {
            builder.ToTable("suppliers");
            builder.HasKey(supplier => supplier.Id);
            builder.Property(supplier => supplier.Code).HasMaxLength(30).IsRequired();
            builder.Property(supplier => supplier.Name).HasMaxLength(150).IsRequired();
            builder.Property(supplier => supplier.ContactName).HasMaxLength(150);
            builder.Property(supplier => supplier.Email).HasMaxLength(150);
            builder.Property(supplier => supplier.Phone).HasMaxLength(50);
            builder.HasIndex(supplier => supplier.Code).IsUnique();
        });

        modelBuilder.Entity<Product>(builder =>
        {
            builder.ToTable("products");
            builder.HasKey(product => product.Id);
            builder.Property(product => product.Sku).HasMaxLength(50).IsRequired();
            builder.Property(product => product.Name).HasMaxLength(150).IsRequired();
            builder.Property(product => product.Description).HasMaxLength(500).IsRequired();
            builder.Property(product => product.UnitPrice).HasPrecision(18, 2);
            builder.Property(product => product.StandardCost).HasPrecision(18, 2);
            builder.HasIndex(product => product.Sku).IsUnique();
            builder.HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(product => product.Supplier)
                .WithMany(supplier => supplier.Products)
                .HasForeignKey(product => product.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Customer>(builder =>
        {
            builder.ToTable("customers");
            builder.HasKey(customer => customer.Id);
            builder.Property(customer => customer.Code).HasMaxLength(30).IsRequired();
            builder.Property(customer => customer.Name).HasMaxLength(150).IsRequired();
            builder.Property(customer => customer.Document).HasMaxLength(40).IsRequired();
            builder.Property(customer => customer.Email).HasMaxLength(150);
            builder.Property(customer => customer.Phone).HasMaxLength(50);
            builder.Property(customer => customer.Status).HasConversion<string>().HasMaxLength(20);
            builder.HasIndex(customer => customer.Code).IsUnique();
            builder.HasIndex(customer => customer.Document).IsUnique();
        });

        modelBuilder.Entity<Branch>(builder =>
        {
            builder.ToTable("branches");
            builder.HasKey(branch => branch.Id);
            builder.Property(branch => branch.Code).HasMaxLength(20).IsRequired();
            builder.Property(branch => branch.Name).HasMaxLength(120).IsRequired();
            builder.Property(branch => branch.LegalName).HasMaxLength(180).IsRequired();
            builder.Property(branch => branch.Document).HasMaxLength(40).IsRequired();
            builder.Property(branch => branch.City).HasMaxLength(120).IsRequired();
            builder.Property(branch => branch.State).HasMaxLength(20).IsRequired();
            builder.HasIndex(branch => branch.Code).IsUnique();
        });

        modelBuilder.Entity<CostCenter>(builder =>
        {
            builder.ToTable("cost_centers");
            builder.HasKey(costCenter => costCenter.Id);
            builder.Property(costCenter => costCenter.Code).HasMaxLength(30).IsRequired();
            builder.Property(costCenter => costCenter.Name).HasMaxLength(120).IsRequired();
            builder.Property(costCenter => costCenter.Description).HasMaxLength(255).IsRequired();
            builder.HasIndex(costCenter => new { costCenter.BranchId, costCenter.Code }).IsUnique();
            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(costCenter => costCenter.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ModuleFeatureFlag>(builder =>
        {
            builder.ToTable("module_feature_flags");
            builder.HasKey(flag => flag.Id);
            builder.Property(flag => flag.ModuleKey).HasMaxLength(50).IsRequired();
            builder.Property(flag => flag.DisplayName).HasMaxLength(120).IsRequired();
            builder.Property(flag => flag.Description).HasMaxLength(255).IsRequired();
            builder.HasIndex(flag => new { flag.ModuleKey, flag.BranchId }).IsUnique();
            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(flag => flag.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ApprovalRule>(builder =>
        {
            builder.ToTable("approval_rules");
            builder.HasKey(rule => rule.Id);
            builder.Property(rule => rule.Name).HasMaxLength(120).IsRequired();
            builder.Property(rule => rule.Module).HasConversion<string>().HasMaxLength(40);
            builder.Property(rule => rule.MinimumAmount).HasPrecision(18, 2);
            builder.Property(rule => rule.RequiredRoleName).HasMaxLength(80).IsRequired();
            builder.Property(rule => rule.Description).HasMaxLength(255).IsRequired();
            builder.HasIndex(rule => new { rule.Module, rule.BranchId, rule.MinimumAmount });
            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(rule => rule.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ApprovalDecision>(builder =>
        {
            builder.ToTable("approval_decisions");
            builder.HasKey(decision => decision.Id);
            builder.Property(decision => decision.Module).HasConversion<string>().HasMaxLength(40);
            builder.Property(decision => decision.Status).HasConversion<string>().HasMaxLength(30);
            builder.Property(decision => decision.ReferenceNumber).HasMaxLength(50).IsRequired();
            builder.Property(decision => decision.RequestedAmount).HasPrecision(18, 2);
            builder.Property(decision => decision.RequestedByName).HasMaxLength(150);
            builder.Property(decision => decision.DecisionByName).HasMaxLength(150);
            builder.Property(decision => decision.Notes).HasMaxLength(500);
            builder.HasIndex(decision => new { decision.Module, decision.EntityId, decision.Status });
            builder.HasOne<ApprovalRule>()
                .WithMany()
                .HasForeignKey(decision => decision.ApprovalRuleId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(decision => decision.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<CostCenter>()
                .WithMany()
                .HasForeignKey(decision => decision.CostCenterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RealtimeNotification>(builder =>
        {
            builder.ToTable("realtime_notifications");
            builder.HasKey(notification => notification.Id);
            builder.Property(notification => notification.RoutingKey).HasMaxLength(120).IsRequired();
            builder.Property(notification => notification.Source).HasMaxLength(80).IsRequired();
            builder.Property(notification => notification.Title).HasMaxLength(180).IsRequired();
            builder.Property(notification => notification.Message).HasMaxLength(500).IsRequired();
            builder.Property(notification => notification.Severity).HasConversion<string>().HasMaxLength(20);
            builder.Property(notification => notification.MetadataJson).HasColumnType("jsonb");
            builder.HasIndex(notification => new { notification.IsRead, notification.PublishedAtUtc });
            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(notification => notification.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(notification => notification.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalesOrder>(builder =>
        {
            builder.ToTable("sales_orders");
            builder.HasKey(order => order.Id);
            builder.Property(order => order.OrderNumber).HasMaxLength(40).IsRequired();
            builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(order => order.ApprovalStatus).HasConversion<string>().HasMaxLength(20);
            builder.Property(order => order.Notes).HasMaxLength(500);
            builder.Property(order => order.TotalAmount).HasPrecision(18, 2);
            builder.HasIndex(order => order.OrderNumber).IsUnique();
            builder.HasOne(order => order.Customer)
                .WithMany(customer => customer.SalesOrders)
                .HasForeignKey(order => order.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(order => order.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<CostCenter>()
                .WithMany()
                .HasForeignKey(order => order.CostCenterId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<ApprovalRule>()
                .WithMany()
                .HasForeignKey(order => order.ApprovalRuleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalesOrderItem>(builder =>
        {
            builder.ToTable("sales_order_items");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Sku).HasMaxLength(50).IsRequired();
            builder.Property(item => item.ProductName).HasMaxLength(150).IsRequired();
            builder.Property(item => item.UnitPrice).HasPrecision(18, 2);
            builder.Property(item => item.UnitCost).HasPrecision(18, 2);
            builder.Property(item => item.LineTotal).HasPrecision(18, 2);
            builder.Property(item => item.LineCost).HasPrecision(18, 2);
            builder.HasOne(item => item.SalesOrder)
                .WithMany(order => order.Items)
                .HasForeignKey(item => item.SalesOrderId);
        });

        modelBuilder.Entity<PurchaseOrder>(builder =>
        {
            builder.ToTable("purchase_orders");
            builder.HasKey(order => order.Id);
            builder.Property(order => order.OrderNumber).HasMaxLength(40).IsRequired();
            builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(30);
            builder.Property(order => order.ApprovalStatus).HasConversion<string>().HasMaxLength(20);
            builder.Property(order => order.Notes).HasMaxLength(500);
            builder.Property(order => order.TotalAmount).HasPrecision(18, 2);
            builder.HasIndex(order => order.OrderNumber).IsUnique();
            builder.HasOne<Supplier>()
                .WithMany()
                .HasForeignKey(order => order.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<WarehouseLocation>()
                .WithMany()
                .HasForeignKey(order => order.WarehouseLocationId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(order => order.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<CostCenter>()
                .WithMany()
                .HasForeignKey(order => order.CostCenterId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<ApprovalRule>()
                .WithMany()
                .HasForeignKey(order => order.ApprovalRuleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseOrderItem>(builder =>
        {
            builder.ToTable("purchase_order_items");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Sku).HasMaxLength(50).IsRequired();
            builder.Property(item => item.ProductName).HasMaxLength(150).IsRequired();
            builder.Property(item => item.UnitCost).HasPrecision(18, 2);
            builder.Property(item => item.LineTotal).HasPrecision(18, 2);
            builder.HasOne(item => item.PurchaseOrder)
                .WithMany(order => order.Items)
                .HasForeignKey(item => item.PurchaseOrderId);
            builder.HasOne<Product>()
                .WithMany()
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseReceipt>(builder =>
        {
            builder.ToTable("purchase_receipts");
            builder.HasKey(receipt => receipt.Id);
            builder.Property(receipt => receipt.ReceiptNumber).HasMaxLength(50).IsRequired();
            builder.Property(receipt => receipt.TotalAmount).HasPrecision(18, 2);
            builder.Property(receipt => receipt.Notes).HasMaxLength(500);
            builder.HasIndex(receipt => receipt.ReceiptNumber).IsUnique();
            builder.HasOne(receipt => receipt.PurchaseOrder)
                .WithMany(order => order.Receipts)
                .HasForeignKey(receipt => receipt.PurchaseOrderId);
        });

        modelBuilder.Entity<PurchaseReceiptItem>(builder =>
        {
            builder.ToTable("purchase_receipt_items");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Sku).HasMaxLength(50).IsRequired();
            builder.Property(item => item.ProductName).HasMaxLength(150).IsRequired();
            builder.Property(item => item.UnitCost).HasPrecision(18, 2);
            builder.Property(item => item.LineTotal).HasPrecision(18, 2);
            builder.HasOne(item => item.PurchaseReceipt)
                .WithMany(receipt => receipt.Items)
                .HasForeignKey(item => item.PurchaseReceiptId);
            builder.HasOne<Product>()
                .WithMany()
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WarehouseLocation>(builder =>
        {
            builder.ToTable("warehouse_locations");
            builder.HasKey(location => location.Id);
            builder.Property(location => location.Code).HasMaxLength(30).IsRequired();
            builder.Property(location => location.Name).HasMaxLength(100).IsRequired();
            builder.Property(location => location.Description).HasMaxLength(255).IsRequired();
            builder.HasIndex(location => location.Code).IsUnique();
            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(location => location.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryBalance>(builder =>
        {
            builder.ToTable("inventory_balances");
            builder.HasKey(balance => balance.Id);
            builder.HasIndex(balance => new { balance.ProductId, balance.WarehouseLocationId }).IsUnique();
        });

        modelBuilder.Entity<StockReservation>(builder =>
        {
            builder.ToTable("stock_reservations");
            builder.HasKey(reservation => reservation.Id);
            builder.Property(reservation => reservation.Status).HasConversion<string>().HasMaxLength(20);
            builder.HasIndex(reservation => new { reservation.SalesOrderId, reservation.ProductId, reservation.WarehouseLocationId });
        });

        modelBuilder.Entity<StockMovement>(builder =>
        {
            builder.ToTable("stock_movements");
            builder.HasKey(movement => movement.Id);
            builder.Property(movement => movement.Type).HasConversion<string>().HasMaxLength(30);
            builder.Property(movement => movement.Reason).HasMaxLength(255).IsRequired();
            builder.Property(movement => movement.ReferenceType).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Invoice>(builder =>
        {
            builder.ToTable("invoices");
            builder.HasKey(invoice => invoice.Id);
            builder.Property(invoice => invoice.InvoiceNumber).HasMaxLength(50).IsRequired();
            builder.Property(invoice => invoice.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(invoice => invoice.TotalAmount).HasPrecision(18, 2);
            builder.Property(invoice => invoice.Notes).HasMaxLength(500);
            builder.HasIndex(invoice => invoice.InvoiceNumber).IsUnique();
            builder.HasIndex(invoice => new { invoice.SalesOrderId, invoice.Status });
            builder.HasOne<SalesOrder>()
                .WithMany()
                .HasForeignKey(invoice => invoice.SalesOrderId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(invoice => invoice.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(invoice => invoice.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<CostCenter>()
                .WithMany()
                .HasForeignKey(invoice => invoice.CostCenterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceItem>(builder =>
        {
            builder.ToTable("invoice_items");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Sku).HasMaxLength(50).IsRequired();
            builder.Property(item => item.ProductName).HasMaxLength(150).IsRequired();
            builder.Property(item => item.UnitPrice).HasPrecision(18, 2);
            builder.Property(item => item.UnitCost).HasPrecision(18, 2);
            builder.Property(item => item.LineTotal).HasPrecision(18, 2);
            builder.Property(item => item.LineCost).HasPrecision(18, 2);
            builder.HasOne(item => item.Invoice)
                .WithMany(invoice => invoice.Items)
                .HasForeignKey(item => item.InvoiceId);
            builder.HasOne<Product>()
                .WithMany()
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Receivable>(builder =>
        {
            builder.ToTable("receivables");
            builder.HasKey(receivable => receivable.Id);
            builder.Property(receivable => receivable.DocumentNumber).HasMaxLength(50).IsRequired();
            builder.Property(receivable => receivable.Description).HasMaxLength(255).IsRequired();
            builder.Property(receivable => receivable.OriginalAmount).HasPrecision(18, 2);
            builder.Property(receivable => receivable.PaidAmount).HasPrecision(18, 2);
            builder.Property(receivable => receivable.OutstandingAmount).HasPrecision(18, 2);
            builder.Property(receivable => receivable.Status).HasConversion<string>().HasMaxLength(20);
            builder.HasIndex(receivable => receivable.DocumentNumber).IsUnique();
            builder.HasIndex(receivable => new { receivable.DueDateUtc, receivable.Status });
            builder.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(receivable => receivable.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Invoice>()
                .WithMany()
                .HasForeignKey(receivable => receivable.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(receivable => receivable.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<CostCenter>()
                .WithMany()
                .HasForeignKey(receivable => receivable.CostCenterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReceivableSettlement>(builder =>
        {
            builder.ToTable("receivable_settlements");
            builder.HasKey(settlement => settlement.Id);
            builder.Property(settlement => settlement.Amount).HasPrecision(18, 2);
            builder.Property(settlement => settlement.PaymentMethod).HasMaxLength(80).IsRequired();
            builder.Property(settlement => settlement.Notes).HasMaxLength(255);
            builder.HasOne(settlement => settlement.Receivable)
                .WithMany(receivable => receivable.Settlements)
                .HasForeignKey(settlement => settlement.ReceivableId);
        });

        modelBuilder.Entity<Payable>(builder =>
        {
            builder.ToTable("payables");
            builder.HasKey(payable => payable.Id);
            builder.Property(payable => payable.DocumentNumber).HasMaxLength(50).IsRequired();
            builder.Property(payable => payable.Description).HasMaxLength(255).IsRequired();
            builder.Property(payable => payable.OriginalAmount).HasPrecision(18, 2);
            builder.Property(payable => payable.PaidAmount).HasPrecision(18, 2);
            builder.Property(payable => payable.OutstandingAmount).HasPrecision(18, 2);
            builder.Property(payable => payable.Status).HasConversion<string>().HasMaxLength(20);
            builder.HasIndex(payable => payable.DocumentNumber).IsUnique();
            builder.HasIndex(payable => new { payable.DueDateUtc, payable.Status });
            builder.HasOne<Supplier>()
                .WithMany()
                .HasForeignKey(payable => payable.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<PurchaseOrder>()
                .WithMany()
                .HasForeignKey(payable => payable.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<PurchaseReceipt>()
                .WithMany()
                .HasForeignKey(payable => payable.PurchaseReceiptId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(payable => payable.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<CostCenter>()
                .WithMany()
                .HasForeignKey(payable => payable.CostCenterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PayableSettlement>(builder =>
        {
            builder.ToTable("payable_settlements");
            builder.HasKey(settlement => settlement.Id);
            builder.Property(settlement => settlement.Amount).HasPrecision(18, 2);
            builder.Property(settlement => settlement.PaymentMethod).HasMaxLength(80).IsRequired();
            builder.Property(settlement => settlement.Notes).HasMaxLength(255);
            builder.HasOne(settlement => settlement.Payable)
                .WithMany(payable => payable.Settlements)
                .HasForeignKey(settlement => settlement.PayableId);
        });

        modelBuilder.Entity<AuditEntry>(builder =>
        {
            builder.ToTable("audit_entries");
            builder.HasKey(audit => audit.Id);
            builder.Property(audit => audit.EventType).HasMaxLength(100).IsRequired();
            builder.Property(audit => audit.EntityName).HasMaxLength(100).IsRequired();
            builder.Property(audit => audit.EntityId).HasMaxLength(120).IsRequired();
            builder.Property(audit => audit.ActorName).HasMaxLength(150);
            builder.Property(audit => audit.MetadataJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<OperationalAlert>(builder =>
        {
            builder.ToTable("operational_alerts");
            builder.HasKey(alert => alert.Id);
            builder.Property(alert => alert.Code).HasMaxLength(120).IsRequired();
            builder.Property(alert => alert.Title).HasMaxLength(180).IsRequired();
            builder.Property(alert => alert.Message).HasMaxLength(500).IsRequired();
            builder.Property(alert => alert.Source).HasMaxLength(120).IsRequired();
            builder.Property(alert => alert.ReferenceId).HasMaxLength(120);
            builder.Property(alert => alert.Severity).HasConversion<string>().HasMaxLength(20);
            builder.Property(alert => alert.Status).HasConversion<string>().HasMaxLength(20);
            builder.HasIndex(alert => new { alert.Code, alert.Status });
            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(alert => alert.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.SetCreatedAt(utcNow);
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.Touch(utcNow);
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
