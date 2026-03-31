using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Praxis.Application.Models;
using Praxis.Application.Services;
using Praxis.Domain.Identity;

namespace Praxis.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public sealed class CustomersController(CustomerService customerService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCatalog.CustomerRead)]
    public async Task<ActionResult<IReadOnlyCollection<CustomerResponse>>> ListAsync(CancellationToken cancellationToken)
    {
        return Ok(await customerService.ListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.CustomerWrite)]
    public async Task<ActionResult<CustomerResponse>> CreateAsync([FromBody] CustomerUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await customerService.CreateAsync(request, DateTime.UtcNow, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.CustomerWrite)]
    public async Task<ActionResult<CustomerResponse>> UpdateAsync(Guid id, [FromBody] CustomerUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await customerService.UpdateAsync(id, request, DateTime.UtcNow, cancellationToken));
    }
}
