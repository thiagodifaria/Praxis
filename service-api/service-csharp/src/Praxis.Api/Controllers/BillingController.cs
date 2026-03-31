using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Praxis.Application.Models;
using Praxis.Application.Services;
using Praxis.Domain.Identity;

namespace Praxis.Api.Controllers;

[ApiController]
[Route("api/billing")]
[Authorize]
public sealed class BillingController(BillingService billingService, FinancialService financialService) : ControllerBase
{
    [HttpGet("invoices")]
    [Authorize(Policy = PermissionCatalog.BillingRead)]
    public async Task<ActionResult<IReadOnlyCollection<InvoiceResponse>>> ListInvoicesAsync(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? costCenterId,
        CancellationToken cancellationToken)
    {
        return Ok(await billingService.ListInvoicesAsync(branchId, costCenterId, cancellationToken));
    }

    [HttpGet("invoices/{id:guid}")]
    [Authorize(Policy = PermissionCatalog.BillingRead)]
    public async Task<ActionResult<InvoiceResponse>> GetInvoiceAsync(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await billingService.GetInvoiceAsync(id, cancellationToken));
    }

    [HttpPost("invoices")]
    [Authorize(Policy = PermissionCatalog.BillingWrite)]
    public async Task<ActionResult<InvoiceResponse>> IssueInvoiceAsync([FromBody] IssueInvoiceRequest request, CancellationToken cancellationToken)
    {
        return Ok(await billingService.IssueInvoiceAsync(request, DateTime.UtcNow, cancellationToken));
    }

    [HttpPost("invoices/{id:guid}/cancel")]
    [Authorize(Policy = PermissionCatalog.BillingWrite)]
    public async Task<ActionResult<InvoiceResponse>> CancelInvoiceAsync(Guid id, [FromBody] OrderStatusTransitionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await billingService.CancelInvoiceAsync(id, request.Notes, DateTime.UtcNow, cancellationToken));
    }

    [HttpGet("receivables")]
    [Authorize(Policy = PermissionCatalog.BillingRead)]
    public async Task<ActionResult<IReadOnlyCollection<ReceivableResponse>>> ListReceivablesAsync(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? costCenterId,
        CancellationToken cancellationToken)
    {
        return Ok(await financialService.ListReceivablesAsync(branchId, costCenterId, cancellationToken));
    }

    [HttpPost("receivables/{id:guid}/settlements")]
    [Authorize(Policy = PermissionCatalog.BillingWrite)]
    public async Task<ActionResult<ReceivableResponse>> RegisterReceivableSettlementAsync(Guid id, [FromBody] FinancialSettlementRequest request, CancellationToken cancellationToken)
    {
        return Ok(await financialService.RegisterReceivableSettlementAsync(id, request, DateTime.UtcNow, cancellationToken));
    }

    [HttpGet("payables")]
    [Authorize(Policy = PermissionCatalog.BillingRead)]
    public async Task<ActionResult<IReadOnlyCollection<PayableResponse>>> ListPayablesAsync(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? costCenterId,
        CancellationToken cancellationToken)
    {
        return Ok(await financialService.ListPayablesAsync(branchId, costCenterId, cancellationToken));
    }

    [HttpPost("payables/{id:guid}/settlements")]
    [Authorize(Policy = PermissionCatalog.BillingWrite)]
    public async Task<ActionResult<PayableResponse>> RegisterPayableSettlementAsync(Guid id, [FromBody] FinancialSettlementRequest request, CancellationToken cancellationToken)
    {
        return Ok(await financialService.RegisterPayableSettlementAsync(id, request, DateTime.UtcNow, cancellationToken));
    }
}
