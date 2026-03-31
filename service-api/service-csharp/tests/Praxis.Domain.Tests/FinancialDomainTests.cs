using Praxis.Domain.Billing;
using Praxis.Domain.Purchasing;

namespace Praxis.Domain.Tests;

public sealed class FinancialDomainTests
{
    [Fact]
    public void PurchaseOrder_ShouldBecomeReceived_WhenReceiptCoversAllItems()
    {
        var now = DateTime.UtcNow;
        var order = new PurchaseOrder("PO-TEST-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, now.AddDays(5), "full receipt");
        order.AddItem(Guid.NewGuid(), "SKU-1", "Keyboard", 10, 50m);
        order.Approve(now);

        var receipt = new PurchaseReceipt(order.Id, "RCV-TEST-001", now.AddDays(1), "received", Guid.NewGuid());
        receipt.AddItem(order.Items.First().ProductId, "SKU-1", "Keyboard", 10, 50m);
        order.Items.First().RegisterReceipt(10, now.AddDays(1));
        order.RegisterReceipt(receipt, now.AddDays(1));

        Assert.Equal(PurchaseOrderStatus.Received, order.Status);
        Assert.NotNull(order.ReceivedAtUtc);
    }

    [Fact]
    public void Receivable_ShouldBecomePaid_WhenSettlementMatchesOutstandingAmount()
    {
        var now = DateTime.UtcNow;
        var receivable = new Receivable(Guid.NewGuid(), Guid.NewGuid(), null, null, "INV-TEST-001", "invoice", now, now.AddDays(30), 1200m);
        var settlement = new ReceivableSettlement(receivable.Id, 1200m, now.AddDays(1), "pix", "full payment", Guid.NewGuid());

        receivable.ApplySettlement(settlement, now.AddDays(1));

        Assert.Equal(FinancialTitleStatus.Paid, receivable.Status);
        Assert.Equal(0m, receivable.OutstandingAmount);
        Assert.NotNull(receivable.SettledAtUtc);
    }
}
