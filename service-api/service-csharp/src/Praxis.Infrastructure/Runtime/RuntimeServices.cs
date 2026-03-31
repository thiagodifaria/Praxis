using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Praxis.Application.Abstractions;
using Praxis.Application.Models;

namespace Praxis.Infrastructure.Runtime;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = "service-redis:6379";
    public string DashboardCacheKey { get; set; } = "praxis:dashboard:snapshot";
    public int DashboardCacheMinutes { get; set; } = 5;
}

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public CurrentUserContext GetCurrentUser()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return new CurrentUserContext(null, null, null, null, Array.Empty<string>());
        }

        var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue(ClaimTypes.Name)
            ?? httpContext.User.FindFirstValue("sub");

        Guid? userId = Guid.TryParse(userIdClaim, out var parsed) ? parsed : null;

        return new CurrentUserContext(
            userId,
            httpContext.User.FindFirstValue(ClaimTypes.Email) ?? httpContext.User.FindFirstValue("email"),
            httpContext.User.FindFirstValue("name") ?? httpContext.User.Identity?.Name,
            httpContext.User.FindFirstValue(ClaimTypes.Role) ?? httpContext.User.FindFirstValue("role"),
            httpContext.User.FindAll("permission").Select(claim => claim.Value).ToArray());
    }
}

public sealed class RedisDashboardCache(IDistributedCache distributedCache, IOptions<RedisOptions> options) : IDashboardCache
{
    private readonly RedisOptions _options = options.Value;

    public async Task<DashboardSnapshotResponse?> GetAsync(CancellationToken cancellationToken = default)
    {
        var payload = await distributedCache.GetStringAsync(_options.DashboardCacheKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        return JsonSerializer.Deserialize<DashboardSnapshotResponse>(payload);
    }

    public Task SetAsync(DashboardSnapshotResponse snapshot, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(snapshot);

        return distributedCache.SetStringAsync(
            _options.DashboardCacheKey,
            payload,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.DashboardCacheMinutes)
            },
            cancellationToken);
    }

    public Task RemoveAsync(CancellationToken cancellationToken = default)
    {
        return distributedCache.RemoveAsync(_options.DashboardCacheKey, cancellationToken);
    }
}
