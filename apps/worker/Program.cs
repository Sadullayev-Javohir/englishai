using Application;
using Infrastructure;
using Infrastructure.Diagnostics;
using Infrastructure.Jobs;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Worker;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(
        Math.Clamp(builder.Configuration.GetValue("Host:ShutdownTimeoutSeconds", 300), 30, 900));
});
builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEnglishAiHangfireWorker(builder.Configuration);
builder.Services.AddScoped<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>();
builder.Services.AddSingleton<HangfireMetrics>();
builder.Services.AddHealthChecks()
    .AddCheck<PostgreSqlHealthCheck>("postgresql", tags: ["ready"])
    .AddCheck<DatabaseSchemaHealthCheck>("database_schema", tags: ["ready"])
    .AddCheck<HangfireStorageHealthCheck>("hangfire_storage", tags: ["ready"]);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        builder.Configuration["OTEL_SERVICE_NAME"] ?? "EnglishAI.Worker",
        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString(),
        serviceInstanceId: Environment.MachineName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options => options.Filter = context =>
            !context.Request.Path.StartsWithSegments("/health") &&
            !context.Request.Path.StartsWithSegments("/metrics"))
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation())
    .WithMetrics(metrics => metrics
        .AddMeter(WorkerTelemetry.MeterName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddPrometheusExporter());

var app = builder.Build();
_ = app.Services.GetRequiredService<HangfireMetrics>();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();
app.UseMiddleware<WorkerMetricsAuthorizationMiddleware>();
app.MapPrometheusScrapingEndpoint("/metrics").AllowAnonymous();
await app.RunAsync();

public partial class Program;
