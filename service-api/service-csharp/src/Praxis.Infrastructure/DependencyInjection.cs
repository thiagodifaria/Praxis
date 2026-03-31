using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Praxis.Application.Abstractions;
using Praxis.Application.Persistence;
using Praxis.Application.Services;
using Praxis.Infrastructure.Jobs;
using Praxis.Infrastructure.Messaging;
using Praxis.Infrastructure.Realtime;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Runtime;
using Praxis.Infrastructure.Security;
using RabbitMQ.Client;
using System.Text;

namespace Praxis.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPraxisPlatform(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PraxisDatabase")
            ?? throw new InvalidOperationException("Connection string 'PraxisDatabase' was not found.");

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));

        services.AddHttpContextAccessor();
        services.AddScoped<AuthService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<CustomerService>();
        services.AddScoped<InventoryService>();
        services.AddScoped<SalesOrderService>();
        services.AddScoped<PurchasingService>();
        services.AddScoped<BillingService>();
        services.AddScoped<FinancialService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<ReportingService>();
        services.AddScoped<OperationsService>();
        services.AddScoped<PlatformService>();
        services.AddScoped<PlatformPolicyService>();
        services.AddScoped<OperationalJobs>();
        services.AddScoped<AppSeeder>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IEventBus, RabbitMqEventBus>();
        services.AddScoped<IDashboardCache, RedisDashboardCache>();

        services.AddDbContext<PraxisDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.EnableSensitiveDataLogging(false);
        });

        services.AddScoped<IPraxisDbContext>(provider => provider.GetRequiredService<PraxisDbContext>());

        var redisOptions = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
        services.AddStackExchangeRedisCache(options => options.Configuration = redisOptions.ConnectionString);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
                };
            });

        services.AddHangfire(configurationBuilder => configurationBuilder
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(storage => storage.UseNpgsqlConnection(connectionString)));

        services.AddHealthChecks().AddCheck<PlatformHealthCheck>("platform");

        return services;
    }

    public static IServiceCollection AddPraxisWorkerRuntime(this IServiceCollection services)
    {
        services.AddHostedService<RabbitMqConsumerService>();
        services.AddHangfireServer(options => options.WorkerCount = 2);

        return services;
    }

    public static IServiceCollection AddPraxisApiRuntime(this IServiceCollection services)
    {
        services.AddHostedService<RabbitMqRealtimeBridgeService>();
        return services;
    }
}

internal sealed class PlatformHealthCheck(
    PraxisDbContext dbContext,
    IDistributedCache distributedCache,
    IOptions<RabbitMqOptions> rabbitMqOptions) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Database.CanConnectAsync(cancellationToken))
        {
            return HealthCheckResult.Unhealthy("PostgreSQL is unavailable.");
        }

        try
        {
            await distributedCache.GetStringAsync("praxis:health", cancellationToken);
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Redis is unavailable.", exception);
        }

        try
        {
            var settings = rabbitMqOptions.Value;
            var factory = new ConnectionFactory
            {
                HostName = settings.HostName,
                Port = settings.Port,
                UserName = settings.Username,
                Password = settings.Password
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ is unavailable.", exception);
        }

        return HealthCheckResult.Healthy();
    }
}
