namespace Praxis.Domain.Identity;

public static class PermissionCatalog
{
    public const string IdentityRead = "identity.read";
    public const string IdentityWrite = "identity.write";
    public const string CatalogRead = "catalog.read";
    public const string CatalogWrite = "catalog.write";
    public const string CustomerRead = "customer.read";
    public const string CustomerWrite = "customer.write";
    public const string SalesRead = "sales.read";
    public const string SalesWrite = "sales.write";
    public const string PurchasingRead = "purchasing.read";
    public const string PurchasingWrite = "purchasing.write";
    public const string InventoryRead = "inventory.read";
    public const string InventoryWrite = "inventory.write";
    public const string BillingRead = "billing.read";
    public const string BillingWrite = "billing.write";
    public const string DashboardRead = "dashboard.read";
    public const string ReportingRead = "reporting.read";
    public const string AuditRead = "audit.read";
    public const string OpsManage = "ops.manage";
    public const string ConfigurationRead = "configuration.read";
    public const string ConfigurationWrite = "configuration.write";
    public const string NotificationRead = "notification.read";
    public const string NotificationWrite = "notification.write";

    public static IReadOnlyList<string> All =>
    [
        IdentityRead,
        IdentityWrite,
        CatalogRead,
        CatalogWrite,
        CustomerRead,
        CustomerWrite,
        SalesRead,
        SalesWrite,
        PurchasingRead,
        PurchasingWrite,
        InventoryRead,
        InventoryWrite,
        BillingRead,
        BillingWrite,
        DashboardRead,
        ReportingRead,
        AuditRead,
        OpsManage,
        ConfigurationRead,
        ConfigurationWrite,
        NotificationRead,
        NotificationWrite
    ];
}
