using Praxis.Domain.Sales;

namespace Praxis.Domain.Tests;

public sealed class SalesOrderDomainTests
{
    [Fact]
    public void Approve_ShouldChangeStatus_WhenOrderIsDraft()
    {
        var order = new SalesOrder("SO-TEST-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "draft order");
        order.AddItem(Guid.NewGuid(), "SKU-1", "Keyboard", 2, 100m, 60m);

        order.Approve(DateTime.UtcNow);

        Assert.Equal(SalesOrderStatus.Approved, order.Status);
        Assert.NotNull(order.ApprovedAtUtc);
    }

    [Fact]
    public void Cancel_ShouldThrow_WhenOrderWasAlreadyDispatched()
    {
        var now = DateTime.UtcNow;
        var order = new SalesOrder("SO-TEST-002", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "approved order");
        order.AddItem(Guid.NewGuid(), "SKU-2", "Mouse", 1, 50m, 25m);
        order.Approve(now);
        order.Dispatch(now.AddMinutes(5));

        Assert.Throws<InvalidOperationException>(() => order.Cancel("late cancel", now.AddMinutes(10)));
    }
}
