using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Praxis.Application.Abstractions;
using Praxis.Domain.Operations;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Realtime;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Praxis.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "service-rabbitmq";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "praxis";
    public string Password { get; set; } = "praxis";
    public string ExchangeName { get; set; } = "praxis.events";
    public string QueueName { get; set; } = "praxis.worker.events";
    public string RealtimeQueueName { get; set; } = "praxis.api.realtime";
}

public sealed class RabbitMqEventBus(IOptions<RabbitMqOptions> options) : IEventBus
{
    private readonly RabbitMqOptions _options = options.Value;

    public Task PublishAsync<T>(string routingKey, T payload, CancellationToken cancellationToken = default)
    {
        var factory = CreateFactory(_options);

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);

        var body = JsonSerializer.SerializeToUtf8Bytes(payload);
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        channel.BasicPublish(_options.ExchangeName, routingKey, properties, body);

        return Task.CompletedTask;
    }

    private static ConnectionFactory CreateFactory(RabbitMqOptions options)
    {
        return new ConnectionFactory
        {
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.Username,
            Password = options.Password,
            DispatchConsumersAsync = true
        };
    }
}

public sealed class RabbitMqConsumerService(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConsumerService> logger,
    IClock clock) : BackgroundService
{
    private readonly RabbitMqOptions _options = options.Value;
    private IConnection? _connection;
    private IModel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        _channel.QueueDeclare(_options.QueueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_options.QueueName, _options.ExchangeName, "#");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, eventArgs) =>
        {
            try
            {
                var payload = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                await HandleMessageAsync(eventArgs.RoutingKey, payload, stoppingToken);
                _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to consume RabbitMQ message with routing key {RoutingKey}", eventArgs.RoutingKey);
                _channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel.BasicConsume(_options.QueueName, autoAck: false, consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Dispose();
        _connection?.Dispose();
        return base.StopAsync(cancellationToken);
    }

    private async Task HandleMessageAsync(string routingKey, string payload, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PraxisDbContext>();

        switch (routingKey)
        {
            case "inventory.low-stock.detected":
                await HandleLowStockAsync(dbContext, payload, cancellationToken);
                break;
            case "sales.order.pending-approval":
                await HandlePendingApprovalAsync(dbContext, payload, "SALES-PENDING-APPROVAL", "Pedido aguardando aprovacao", "sales", cancellationToken);
                break;
            case "sales.order.approved":
                await HandleOrderEventAsync(dbContext, payload, "ORDER-APPROVED", "Pedido aprovado", AlertSeverity.Info, cancellationToken);
                break;
            case "sales.order.dispatched":
                await HandleOrderEventAsync(dbContext, payload, "ORDER-DISPATCHED", "Pedido expedido", AlertSeverity.Info, cancellationToken);
                break;
            case "sales.order.rejected":
                await HandleGenericProcessAlertAsync(dbContext, payload, "ORDER-REJECTED", "Pedido rejeitado", "sales", AlertSeverity.Critical, cancellationToken);
                break;
            case "purchasing.order.pending-approval":
                await HandlePendingApprovalAsync(dbContext, payload, "PURCHASE-PENDING-APPROVAL", "Compra aguardando aprovacao", "purchasing", cancellationToken);
                break;
            case "purchasing.order.approved":
                await HandleGenericProcessAlertAsync(dbContext, payload, "PURCHASE-APPROVED", "Compra aprovada", "purchasing", AlertSeverity.Info, cancellationToken);
                break;
            case "purchasing.order.received":
                await HandlePurchaseReceiptAsync(dbContext, payload, cancellationToken);
                break;
            case "purchasing.order.rejected":
                await HandleGenericProcessAlertAsync(dbContext, payload, "PURCHASE-REJECTED", "Compra rejeitada", "purchasing", AlertSeverity.Critical, cancellationToken);
                break;
            case "billing.invoice.issued":
                await HandleInvoiceIssuedAsync(dbContext, payload, cancellationToken);
                break;
            case "finance.receivable.settled":
                await HandleFinancialSettlementAsync(dbContext, payload, "RECEIVABLE-SETTLED", "Recebivel liquidado", "billing", cancellationToken);
                break;
            case "finance.payable.settled":
                await HandleFinancialSettlementAsync(dbContext, payload, "PAYABLE-SETTLED", "Pagamento a fornecedor registrado", "billing", cancellationToken);
                break;
            case "finance.receivable.overdue.detected":
                await HandleOverdueFinancialAlertAsync(dbContext, payload, "RECEIVABLE-OVERDUE", "Recebivel em atraso", "billing", cancellationToken);
                break;
            case "finance.payable.overdue.detected":
                await HandleOverdueFinancialAlertAsync(dbContext, payload, "PAYABLE-OVERDUE", "Pagamento em atraso", "billing", cancellationToken);
                break;
            case "settings.feature-flag.updated":
                await HandleGenericProcessAlertAsync(dbContext, payload, "FEATURE-FLAG-UPDATED", "Feature flag atualizada", "settings", AlertSeverity.Info, cancellationToken);
                break;
            default:
                break;
        }

        await PersistNotificationAsync(dbContext, routingKey, payload, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task PersistNotificationAsync(PraxisDbContext dbContext, string routingKey, string payload, CancellationToken cancellationToken)
    {
        if (!RealtimeNotificationPayloadFactory.TryCreate(routingKey, payload, clock.UtcNow, out var message) || message is null)
        {
            return;
        }

        var recentExists = await dbContext.RealtimeNotifications
            .AnyAsync(
                notification =>
                    notification.RoutingKey == message.RoutingKey &&
                    notification.Message == message.Message &&
                    notification.BranchId == message.BranchId &&
                    notification.PublishedAtUtc >= clock.UtcNow.AddMinutes(-30),
                cancellationToken);

        if (recentExists)
        {
            return;
        }

        var notification = new RealtimeNotification(
            message.RoutingKey,
            message.Source,
            message.Title,
            message.Message,
            message.Severity,
            message.BranchId,
            null,
            message.MetadataJson,
            message.PublishedAtUtc);

        notification.SetCreatedAt(message.PublishedAtUtc);
        dbContext.RealtimeNotifications.Add(notification);
    }

    private async Task HandleLowStockAsync(PraxisDbContext dbContext, string payload, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        var productId = root.GetProperty("ProductId").GetGuid();
        var warehouseId = root.GetProperty("WarehouseId").GetGuid();
        var branchId = TryGetGuid(root, "BranchId");
        var productName = root.GetProperty("ProductName").GetString() ?? "Unknown product";
        var warehouseName = root.GetProperty("WarehouseName").GetString() ?? "Unknown warehouse";
        var availableQuantity = root.GetProperty("AvailableQuantity").GetInt32();
        var reorderLevel = root.GetProperty("ReorderLevel").GetInt32();

        var code = $"LOW-STOCK-{productId:N}-{warehouseId:N}";
        var existingAlert = await dbContext.OperationalAlerts
            .FirstOrDefaultAsync(alert => alert.Code == code && alert.Status == AlertStatus.Open, cancellationToken);

        if (existingAlert is not null)
        {
            return;
        }

        var alert = new OperationalAlert(
            code,
            "Estoque abaixo do minimo",
            $"{productName} em {warehouseName} esta com saldo {availableQuantity} para minimo {reorderLevel}.",
            "inventory",
            branchId,
            productId.ToString(),
            AlertSeverity.Warning);

        alert.SetCreatedAt(clock.UtcNow);

        dbContext.OperationalAlerts.Add(alert);
        dbContext.AuditEntries.Add(new AuditEntry(
            "inventory.low-stock.alerted",
            nameof(OperationalAlert),
            alert.Id.ToString(),
            null,
            "worker",
            payload));
    }

    private async Task HandlePendingApprovalAsync(
        PraxisDbContext dbContext,
        string payload,
        string codePrefix,
        string title,
        string source,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        var entityId = root.GetProperty("Id").GetGuid();
        var branchId = TryGetGuid(root, "BranchId");
        var documentNumber = root.GetProperty("OrderNumber").GetString() ?? entityId.ToString();
        var requiredRoleName = root.TryGetProperty("RequiredRoleName", out var roleElement) ? roleElement.GetString() ?? "role" : "role";

        var code = $"{codePrefix}-{entityId:N}";
        var existingAlert = await dbContext.OperationalAlerts
            .FirstOrDefaultAsync(alert => alert.Code == code && alert.Status == AlertStatus.Open, cancellationToken);

        if (existingAlert is not null)
        {
            return;
        }

        var alert = new OperationalAlert(
            code,
            title,
            $"{documentNumber} exige aprovacao do perfil {requiredRoleName}.",
            source,
            branchId,
            entityId.ToString(),
            AlertSeverity.Warning);

        alert.SetCreatedAt(clock.UtcNow);
        dbContext.OperationalAlerts.Add(alert);
    }

    private async Task HandleOrderEventAsync(
        PraxisDbContext dbContext,
        string payload,
        string codePrefix,
        string title,
        AlertSeverity severity,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        var orderId = root.GetProperty("Id").GetGuid();
        var branchId = TryGetGuid(root, "BranchId");
        var orderNumber = root.GetProperty("OrderNumber").GetString() ?? orderId.ToString();
        var customerName = root.GetProperty("CustomerName").GetString() ?? "Cliente";
        var warehouseName = root.GetProperty("WarehouseName").GetString() ?? "Deposito";

        var code = $"{codePrefix}-{orderId:N}";
        var existingAlert = await dbContext.OperationalAlerts
            .FirstOrDefaultAsync(alert => alert.Code == code && alert.Status == AlertStatus.Open, cancellationToken);

        if (existingAlert is not null)
        {
            return;
        }

        var alert = new OperationalAlert(
            code,
            title,
            $"{orderNumber} para {customerName} foi processado para {warehouseName}.",
            "sales",
            branchId,
            orderId.ToString(),
            severity);

        alert.SetCreatedAt(clock.UtcNow);

        dbContext.OperationalAlerts.Add(alert);
        dbContext.AuditEntries.Add(new AuditEntry(
            "sales.integration-event.consumed",
            nameof(OperationalAlert),
            alert.Id.ToString(),
            null,
            "worker",
            payload));
    }

    private async Task HandleGenericProcessAlertAsync(
        PraxisDbContext dbContext,
        string payload,
        string codePrefix,
        string title,
        string source,
        AlertSeverity severity,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        var entityId = root.GetProperty("Id").GetGuid();
        var branchId = TryGetGuid(root, "BranchId");
        var documentNumber = root.TryGetProperty("OrderNumber", out var orderNumberElement)
            ? orderNumberElement.GetString() ?? entityId.ToString()
            : root.TryGetProperty("InvoiceNumber", out var invoiceNumberElement)
                ? invoiceNumberElement.GetString() ?? entityId.ToString()
                : entityId.ToString();

        var code = $"{codePrefix}-{entityId:N}";
        var existingAlert = await dbContext.OperationalAlerts
            .FirstOrDefaultAsync(alert => alert.Code == code && alert.Status == AlertStatus.Open, cancellationToken);

        if (existingAlert is not null)
        {
            return;
        }

        var alert = new OperationalAlert(
            code,
            title,
            $"{documentNumber} foi atualizado no fluxo de {source}.",
            source,
            branchId,
            entityId.ToString(),
            severity);

        alert.SetCreatedAt(clock.UtcNow);

        dbContext.OperationalAlerts.Add(alert);
        dbContext.AuditEntries.Add(new AuditEntry(
            $"{source}.integration-event.consumed",
            nameof(OperationalAlert),
            alert.Id.ToString(),
            null,
            "worker",
            payload));
    }

    private async Task HandlePurchaseReceiptAsync(PraxisDbContext dbContext, string payload, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        var orderId = root.GetProperty("Id").GetGuid();
        var branchId = TryGetGuid(root, "BranchId");
        var orderNumber = root.GetProperty("OrderNumber").GetString() ?? orderId.ToString();
        var receiptNumber = root.GetProperty("ReceiptNumber").GetString() ?? "receipt";
        var supplierName = root.GetProperty("SupplierName").GetString() ?? "Fornecedor";
        var warehouseName = root.GetProperty("WarehouseName").GetString() ?? "Deposito";

        var code = $"PURCHASE-RECEIVED-{orderId:N}";
        var existingAlert = await dbContext.OperationalAlerts
            .FirstOrDefaultAsync(alert => alert.Code == code && alert.Status == AlertStatus.Open, cancellationToken);

        if (existingAlert is not null)
        {
            return;
        }

        var alert = new OperationalAlert(
            code,
            "Compra recebida",
            $"{orderNumber} recebeu {receiptNumber} de {supplierName} para {warehouseName}.",
            "purchasing",
            branchId,
            orderId.ToString(),
            AlertSeverity.Info);

        alert.SetCreatedAt(clock.UtcNow);

        dbContext.OperationalAlerts.Add(alert);
        dbContext.AuditEntries.Add(new AuditEntry(
            "purchasing.integration-event.consumed",
            nameof(OperationalAlert),
            alert.Id.ToString(),
            null,
            "worker",
            payload));
    }

    private async Task HandleInvoiceIssuedAsync(PraxisDbContext dbContext, string payload, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        var invoiceId = root.GetProperty("Id").GetGuid();
        var branchId = TryGetGuid(root, "BranchId");
        var invoiceNumber = root.GetProperty("InvoiceNumber").GetString() ?? invoiceId.ToString();
        var orderNumber = root.GetProperty("OrderNumber").GetString() ?? "pedido";
        var customerName = root.GetProperty("CustomerName").GetString() ?? "Cliente";

        var code = $"INVOICE-ISSUED-{invoiceId:N}";
        var existingAlert = await dbContext.OperationalAlerts
            .FirstOrDefaultAsync(alert => alert.Code == code && alert.Status == AlertStatus.Open, cancellationToken);

        if (existingAlert is not null)
        {
            return;
        }

        var alert = new OperationalAlert(
            code,
            "Fatura emitida",
            $"{invoiceNumber} foi emitida para {customerName} a partir de {orderNumber}.",
            "billing",
            branchId,
            invoiceId.ToString(),
            AlertSeverity.Info);

        alert.SetCreatedAt(clock.UtcNow);

        dbContext.OperationalAlerts.Add(alert);
        dbContext.AuditEntries.Add(new AuditEntry(
            "billing.integration-event.consumed",
            nameof(OperationalAlert),
            alert.Id.ToString(),
            null,
            "worker",
            payload));
    }

    private async Task HandleFinancialSettlementAsync(
        PraxisDbContext dbContext,
        string payload,
        string codePrefix,
        string title,
        string source,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        var titleId = root.GetProperty("Id").GetGuid();
        var branchId = TryGetGuid(root, "BranchId");
        var documentNumber = root.GetProperty("DocumentNumber").GetString() ?? titleId.ToString();
        var amount = root.GetProperty("Amount").GetDecimal();

        var code = $"{codePrefix}-{titleId:N}";
        var existingAlert = await dbContext.OperationalAlerts
            .FirstOrDefaultAsync(alert => alert.Code == code && alert.Status == AlertStatus.Open, cancellationToken);

        if (existingAlert is not null)
        {
            return;
        }

        var alert = new OperationalAlert(
            code,
            title,
            $"{documentNumber} registrou liquidacao no valor de {amount:N2}.",
            source,
            branchId,
            titleId.ToString(),
            AlertSeverity.Info);

        alert.SetCreatedAt(clock.UtcNow);

        dbContext.OperationalAlerts.Add(alert);
        dbContext.AuditEntries.Add(new AuditEntry(
            $"{source}.integration-event.consumed",
            nameof(OperationalAlert),
            alert.Id.ToString(),
            null,
            "worker",
            payload));
    }

    private async Task HandleOverdueFinancialAlertAsync(
        PraxisDbContext dbContext,
        string payload,
        string codePrefix,
        string title,
        string source,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        var titleId = root.GetProperty("Id").GetGuid();
        var branchId = TryGetGuid(root, "BranchId");
        var documentNumber = root.GetProperty("DocumentNumber").GetString() ?? titleId.ToString();
        var outstandingAmount = root.GetProperty("OutstandingAmount").GetDecimal();
        var dueDateUtc = root.GetProperty("DueDateUtc").GetDateTime();

        var code = $"{codePrefix}-{titleId:N}";
        var existingAlert = await dbContext.OperationalAlerts
            .FirstOrDefaultAsync(alert => alert.Code == code && alert.Status == AlertStatus.Open, cancellationToken);

        if (existingAlert is not null)
        {
            return;
        }

        var alert = new OperationalAlert(
            code,
            title,
            $"{documentNumber} esta vencido desde {dueDateUtc:yyyy-MM-dd} com saldo {outstandingAmount:N2}.",
            source,
            branchId,
            titleId.ToString(),
            AlertSeverity.Warning);

        alert.SetCreatedAt(clock.UtcNow);

        dbContext.OperationalAlerts.Add(alert);
        dbContext.AuditEntries.Add(new AuditEntry(
            $"{source}.integration-event.consumed",
            nameof(OperationalAlert),
            alert.Id.ToString(),
            null,
            "worker",
            payload));
    }

    private static Guid? TryGetGuid(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.String && Guid.TryParse(element.GetString(), out var parsed))
        {
            return parsed;
        }

        return element.TryGetGuid(out var guid) ? guid : null;
    }
}
