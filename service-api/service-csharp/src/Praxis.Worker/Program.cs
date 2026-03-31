using Hangfire;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Praxis.Infrastructure;
using Praxis.Infrastructure.Jobs;
using Praxis.Infrastructure.Persistence;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPraxisPlatform(builder.Configuration);
builder.Services.AddPraxisWorkerRuntime();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);

var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("praxis-worker"))
    .WithTracing(tracing =>
    {
        tracing.AddHttpClientInstrumentation();

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            metrics.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
        }
    });

var host = builder.Build();

await DatabaseBootstrap.InitializeAsync(host.Services);

using (var scope = host.Services.CreateScope())
{
    var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    recurringJobs.AddOrUpdate<OperationalJobs>(
        "refresh-dashboard-snapshot",
        job => job.RefreshDashboardSnapshotAsync(),
        "*/5 * * * *");

    recurringJobs.AddOrUpdate<OperationalJobs>(
        "scan-low-stock",
        job => job.ScanLowStockAsync(),
        "*/10 * * * *");

    recurringJobs.AddOrUpdate<OperationalJobs>(
        "scan-financial-titles",
        job => job.ScanFinancialTitlesAsync(),
        "*/15 * * * *");
}

await host.RunAsync();
