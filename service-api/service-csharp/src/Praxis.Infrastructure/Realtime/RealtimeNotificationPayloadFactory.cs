using System.Text.Json;
using Praxis.Application.Models;
using Praxis.Domain.Operations;

namespace Praxis.Infrastructure.Realtime;

internal static class RealtimeNotificationPayloadFactory
{
    public static bool TryCreate(string routingKey, string payload, DateTime publishedAtUtc, out NotificationStreamMessage? notification)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        notification = routingKey switch
        {
            "inventory.low-stock.detected" => new NotificationStreamMessage(
                routingKey,
                "inventory",
                "Estoque em atencao",
                $"{GetString(root, "ProductName", "Produto")} em {GetString(root, "WarehouseName", "deposito")} chegou a {GetInt(root, "AvailableQuantity")} unidades.",
                NotificationSeverity.Warning,
                GetGuid(root, "BranchId"),
                payload,
                publishedAtUtc),
            "sales.order.pending-approval" => new NotificationStreamMessage(
                routingKey,
                "sales",
                "Pedido aguardando aprovacao",
                $"{GetString(root, "OrderNumber", "Pedido")} de {GetString(root, "CustomerName", "cliente")} exige aprovacao.",
                NotificationSeverity.Warning,
                GetGuid(root, "BranchId"),
                payload,
                publishedAtUtc),
            "sales.order.approved" => new NotificationStreamMessage(
                routingKey,
                "sales",
                "Pedido aprovado",
                $"{GetString(root, "OrderNumber", "Pedido")} foi aprovado para {GetString(root, "CustomerName", "cliente")}.",
                NotificationSeverity.Success,
                GetGuid(root, "BranchId"),
                payload,
                publishedAtUtc),
            "sales.order.dispatched" => new NotificationStreamMessage(
                routingKey,
                "sales",
                "Pedido expedido",
                $"{GetString(root, "OrderNumber", "Pedido")} saiu para expedicao.",
                NotificationSeverity.Success,
                GetGuid(root, "BranchId"),
                payload,
                publishedAtUtc),
            "sales.order.rejected" => new NotificationStreamMessage(
                routingKey,
                "sales",
                "Pedido rejeitado",
                $"{GetString(root, "OrderNumber", "Pedido")} foi rejeitado na aprovacao.",
                NotificationSeverity.Critical,
                GetGuid(root, "BranchId"),
                payload,
                publishedAtUtc),
            "purchasing.order.pending-approval" => new NotificationStreamMessage(
                routingKey,
                "purchasing",
                "Compra aguardando aprovacao",
                $"{GetString(root, "OrderNumber", "Compra")} de {GetString(root, "SupplierName", "fornecedor")} exige aprovacao.",
                NotificationSeverity.Warning,
                GetGuid(root, "BranchId"),
                payload,
                publishedAtUtc),
            "purchasing.order.approved" => new NotificationStreamMessage(
                routingKey,
                "purchasing",
                "Compra aprovada",
                $"{GetString(root, "OrderNumber", "Compra")} foi aprovada para {GetString(root, "SupplierName", "fornecedor")}.",
                NotificationSeverity.Success,
                GetGuid(root, "BranchId"),
                payload,
                publishedAtUtc),
            "purchasing.order.received" => new NotificationStreamMessage(
                routingKey,
                "purchasing",
                "Compra recebida",
                $"{GetString(root, "OrderNumber", "Compra")} recebeu {GetString(root, "ReceiptNumber", "recebimento")}.",
                NotificationSeverity.Success,
                GetGuid(root, "BranchId"),
                payload,
                publishedAtUtc),
            "purchasing.order.rejected" => new NotificationStreamMessage(
                routingKey,
                "purchasing",
                "Compra rejeitada",
                $"{GetString(root, "OrderNumber", "Compra")} foi rejeitada na aprovacao.",
                NotificationSeverity.Critical,
                GetGuid(root, "BranchId"),
                payload,
                publishedAtUtc),
            "billing.invoice.issued" => new NotificationStreamMessage(
                routingKey,
                "billing",
                "Fatura emitida",
                $"{GetString(root, "InvoiceNumber", "Fatura")} foi emitida para {GetString(root, "CustomerName", "cliente")}.",
                NotificationSeverity.Success,
                GetGuid(root, "BranchId"),
                payload,
                publishedAtUtc),
            "finance.receivable.settled" => new NotificationStreamMessage(
                routingKey,
                "billing",
                "Recebivel liquidado",
                $"{GetString(root, "DocumentNumber", "Titulo")} recebeu baixa de {GetDecimal(root, "Amount"):N2}.",
                NotificationSeverity.Success,
                GetGuid(root, "BranchId"),
                payload,
                publishedAtUtc),
            "finance.payable.settled" => new NotificationStreamMessage(
                routingKey,
                "billing",
                "Pagamento registrado",
                $"{GetString(root, "DocumentNumber", "Titulo")} recebeu pagamento de {GetDecimal(root, "Amount"):N2}.",
                NotificationSeverity.Info,
                GetGuid(root, "BranchId"),
                payload,
                publishedAtUtc),
            "finance.receivable.overdue.detected" => new NotificationStreamMessage(
                routingKey,
                "billing",
                "Recebivel em atraso",
                $"{GetString(root, "DocumentNumber", "Titulo")} venceu e segue com saldo de {GetDecimal(root, "OutstandingAmount"):N2}.",
                NotificationSeverity.Warning,
                GetGuid(root, "BranchId"),
                payload,
                publishedAtUtc),
            "finance.payable.overdue.detected" => new NotificationStreamMessage(
                routingKey,
                "billing",
                "Pagamento em atraso",
                $"{GetString(root, "DocumentNumber", "Titulo")} venceu e segue com saldo de {GetDecimal(root, "OutstandingAmount"):N2}.",
                NotificationSeverity.Warning,
                GetGuid(root, "BranchId"),
                payload,
                publishedAtUtc),
            "settings.feature-flag.updated" => new NotificationStreamMessage(
                routingKey,
                "settings",
                "Feature flag atualizada",
                $"Modulo {GetString(root, "ModuleKey", "modulo")} foi {(GetBool(root, "IsEnabled") ? "ativado" : "desativado")}.",
                NotificationSeverity.Info,
                GetGuid(root, "BranchId"),
                payload,
                publishedAtUtc),
            _ => null
        };

        return notification is not null;
    }

    private static string GetString(JsonElement root, string propertyName, string fallback)
    {
        return root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString() ?? fallback
            : fallback;
    }

    private static Guid? GetGuid(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var element) &&
               element.ValueKind == JsonValueKind.String &&
               Guid.TryParse(element.GetString(), out var guid)
            ? guid
            : root.TryGetProperty(propertyName, out element) && element.ValueKind == JsonValueKind.Null
                ? null
                : element.ValueKind == JsonValueKind.Undefined
                    ? null
                    : element.TryGetGuid(out guid)
                        ? guid
                        : null;
    }

    private static int GetInt(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var element) && element.TryGetInt32(out var value) ? value : 0;
    }

    private static decimal GetDecimal(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var element) && element.TryGetDecimal(out var value) ? value : 0m;
    }

    private static bool GetBool(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.True
            ? true
            : root.TryGetProperty(propertyName, out element) && element.ValueKind == JsonValueKind.False
                ? false
                : false;
    }
}
