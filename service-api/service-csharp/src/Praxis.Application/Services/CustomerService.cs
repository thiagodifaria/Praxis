using Microsoft.EntityFrameworkCore;
using Praxis.Application.Common;
using Praxis.Application.Models;
using Praxis.Application.Persistence;
using Praxis.Domain.Operations;
using Praxis.Domain.Sales;

namespace Praxis.Application.Services;

public sealed class CustomerService(IPraxisDbContext dbContext, PlatformPolicyService platformPolicyService)
{
    public async Task<IReadOnlyCollection<CustomerResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Customers, cancellationToken: cancellationToken);

        return await dbContext.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.Name)
            .Select(customer => new CustomerResponse(
                customer.Id,
                customer.Code,
                customer.Name,
                customer.Document,
                customer.Email,
                customer.Phone,
                customer.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerResponse> CreateAsync(CustomerUpsertRequest request, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Customers, cancellationToken: cancellationToken);
        await EnsureCustomerCodeIsAvailableAsync(request.Code, null, cancellationToken);

        var customer = new Customer(
            request.Code,
            request.Name,
            request.Document,
            request.Email,
            request.Phone,
            request.Status);

        customer.SetCreatedAt(utcNow);

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CustomerResponse(customer.Id, customer.Code, customer.Name, customer.Document, customer.Email, customer.Phone, customer.Status);
    }

    public async Task<CustomerResponse> UpdateAsync(Guid id, CustomerUpsertRequest request, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Customers, cancellationToken: cancellationToken);
        var customer = await dbContext.Customers.FirstOrDefaultAsync(current => current.Id == id, cancellationToken)
            ?? throw new NotFoundException("Customer not found.");

        await EnsureCustomerCodeIsAvailableAsync(request.Code, id, cancellationToken);
        customer.Update(request.Code, request.Name, request.Document, request.Email, request.Phone, request.Status, utcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CustomerResponse(customer.Id, customer.Code, customer.Name, customer.Document, customer.Email, customer.Phone, customer.Status);
    }

    private async Task EnsureCustomerCodeIsAvailableAsync(string code, Guid? currentId, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var exists = await dbContext.Customers.AnyAsync(customer => customer.Code == normalizedCode && customer.Id != currentId, cancellationToken);

        if (exists)
        {
            throw new ConflictException("Customer code is already in use.");
        }
    }
}
