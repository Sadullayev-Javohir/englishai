using System.Text;
using System.Security.Cryptography.X509Certificates;
using Application;
using Application.Common;
using Application.Speaking.AccentTutors;
using Application.Speaking.SubmitUtterance;
using Infrastructure;
using Infrastructure.Identity;
using Infrastructure.Notifications;
using Infrastructure.Retention;
using Infrastructure.Subscription;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Infrastructure.Diagnostics;
using Infrastructure.Competition;
using Serilog;
using Web;
using Web.Endpoints;
using Web.Hubs;
using Web.Logging;
using Web.Middleware;
using Web.Observability;
using Infrastructure.RateLimiting;
using Infrastructure.Redis;
using Infrastructure.Gamification;
using Infrastructure.Jobs;
using Infrastructure.Persistence;
using Web.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(
        Math.Clamp(builder.Configuration.GetValue("Host:ShutdownTimeoutSeconds", 120), 30, 600));
});
var databaseCommand = args.FirstOrDefault()?.Equals("database", StringComparison.OrdinalIgnoreCase) == true
    ? DatabaseCommand.Parse(args)
    : null;
if (databaseCommand is not null)
{
    var migrationConnection = builder.Configuration.GetConnectionString("MigrationPostgres")
                              ?? builder.Configuration.GetConnectionString("Postgres")
                              ?? throw new InvalidOperationException("Database migration connection is not configured.");
    PostgresConnectionPolicy.ValidateMigration(migrationConnection);
    builder.Configuration["ConnectionStrings:Postgres"] = migrationConnection;
}
var curriculumCommand = args.FirstOrDefault()?.Equals("curriculum", StringComparison.OrdinalIgnoreCase) == true
    ? CurriculumCommand.Parse(args)
    : null;
var leaderboardCommand = args.FirstOrDefault()?.Equals("leaderboard", StringComparison.OrdinalIgnoreCase) == true
    ? LeaderboardCommand.Parse(args)
    : null;

// Shared in-process ring buffer of recent warnings/errors, surfaced on the super-admin server page
// (/admin/server). Created up front so the same instance backs both the Serilog sink (writer) and
// the DI-resolved IRecentLogStore (reader); registered before AddInfrastructure so its TryAdd default
// is overridden by this shared instance.
var recentLogStore = new InMemoryRecentLogStore();
builder.Services.AddSingleton<Application.Admin.Ports.IRecentLogStore>(recentLogStore);

builder.Host.UseSerilog((context, configuration) =>
    configuration
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.Sink(new RecentLogSink(recentLogStore)));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<SpeakingLiveOptions>(
    builder.Configuration.GetSection(SpeakingLiveOptions.SectionName));
builder.Services.AddSingleton<SpeakingLiveTurnRegistry>();
builder.Services.AddScoped<SubmitUtteranceCommandHandler>();
builder.Services.AddScoped<AccentTutorTurnCommandHandler>();
var signalR = builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    // Live Speaking / accent-tutor audio arrives as base64-encoded PCM over the hub. A single
    // AppendAudioChunk carries up to 64 KB of PCM, which base64 inflates to ~86 KB — far above
    // SignalR's 32 KB default receive cap. Left at the default, the server closes the socket
    // ("Connection closed with an error") the instant a learner speaks. Raise the ceiling so one
    // chunk plus its message envelope fits comfortably.
    options.MaximumReceiveMessageSize = 128 * 1024;
    // Live turns can leave the socket quiet for tens of seconds (STT + LLM reply + TTS, or a learner
    // simply listening). Ping idle clients every 15s and don't declare a client dead until 60s of
    // silence, so a slow accent-tutor turn no longer drops the hub mid-conversation. Paired with the
    // client's 60s serverTimeout / 15s keep-alive.
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
});
var redisOptions = builder.Configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
redisOptions.Environment = builder.Environment.EnvironmentName;
if (redisOptions.IsConfigured)
{
    var signalRChannelPrefix = builder.Configuration["SignalR:ChannelPrefix"];
    if (string.IsNullOrWhiteSpace(signalRChannelPrefix))
        signalRChannelPrefix = $"englishai:{builder.Environment.EnvironmentName.ToLowerInvariant()}:signalr";

    signalR.AddStackExchangeRedis(options =>
    {
        options.ConnectionFactory = writer => ConnectRedisAsync(redisOptions.EffectiveCache(), writer);
        options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal(signalRChannelPrefix);
    });
}
builder.Services.AddMemoryCache();
builder.Services.AddSingleton(sp => new Web.Auth.MobileGoogleSignInStore(sp.GetService<IRedisConnectionProvider>()));
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<CorrelationIdHandler>();
builder.Services.ConfigureHttpClientDefaults(http => http.AddHttpMessageHandler<CorrelationIdHandler>());
var distributedRateLimitOptions = builder.Configuration
    .GetSection(DistributedRateLimitOptions.SectionName)
    .Get<DistributedRateLimitOptions>() ?? new DistributedRateLimitOptions();
builder.Services.AddSingleton(distributedRateLimitOptions);

builder.Services.AddSingleton<EnglishAiMetrics>();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseSchemaHealthCheck>("database_schema", tags: ["ready"])
    .AddCheck<PostgreSqlHealthCheck>("postgresql", tags: ["ready"])
    .AddCheck<RedisHealthCheck>("redis", tags: ["ready"])
    .AddCheck<ObjectStorageHealthCheck>("object_storage", tags: ["ready"]);

static async Task<StackExchange.Redis.IConnectionMultiplexer> ConnectRedisAsync(
    RedisEndpointOptions endpoint,
    TextWriter writer)
{
    var configuration = StackExchange.Redis.ConfigurationOptions.Parse(
        string.IsNullOrWhiteSpace(endpoint.SentinelConnectionString)
            ? endpoint.ConnectionString
            : endpoint.SentinelConnectionString);
    if (!string.IsNullOrWhiteSpace(endpoint.SentinelConnectionString) && !string.IsNullOrWhiteSpace(endpoint.ConnectionString))
    {
        var data = StackExchange.Redis.ConfigurationOptions.Parse(endpoint.ConnectionString);
        configuration.User = data.User;
        configuration.Password = data.Password;
        configuration.Ssl = configuration.Ssl || data.Ssl;
    }
    configuration.AbortOnConnectFail = false;
    configuration.ConnectRetry = 5;
    configuration.ConnectTimeout = 3000;
    configuration.SyncTimeout = 3000;
    configuration.AsyncTimeout = 3000;
    configuration.KeepAlive = 30;
    if (string.IsNullOrWhiteSpace(endpoint.ServiceName))
        return await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(configuration, writer);

    configuration.ServiceName = endpoint.ServiceName;
    var sentinel = await StackExchange.Redis.ConnectionMultiplexer.SentinelConnectAsync(configuration, writer);
    return sentinel.GetSentinelMasterConnection(configuration, writer);
}

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        EnglishAiTelemetry.ServiceName,
        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString(),
        serviceInstanceId: Environment.MachineName))
    .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation(options => options.Filter = context =>
                !context.Request.Path.StartsWithSegments("/health") &&
                !context.Request.Path.StartsWithSegments("/metrics"))
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation())
    .WithMetrics(metrics => metrics
        .AddMeter(EnglishAiTelemetry.MeterName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddPrometheusExporter());

// Real-time notification delivery rides SignalR, which lives in the Web layer; register the adapter
// for the Application port here (Infrastructure can't reference the hub).
builder.Services.AddScoped<Application.Notifications.Ports.INotificationRealtimeNotifier,
    Web.Notifications.SignalRNotificationNotifier>();
builder.Services.AddScoped<Application.Support.ISupportRealtimeNotifier, Web.Support.SignalRSupportNotifier>();


// CORS for the native mobile shell (Capacitor). The web SPA is served same-origin and needs
// no CORS; the native app, however, runs from a capacitor://localhost / https://localhost
// origin and calls the production API cross-origin. These origins are allow-listed (extra ones
// can be appended via Cors:AllowedOrigins) and credentials are permitted so both the Bearer
// token and - where it survives - the session cookie work. Order matters: UseCors runs early.
const string MobileCorsPolicy = "mobile";
var corsOrigins = new[] { "capacitor://localhost", "https://localhost", "http://localhost" }
    .Concat(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
    .Distinct()
    .ToArray();
builder.Services.AddCors(options =>
    options.AddPolicy(MobileCorsPolicy, policy => policy
        .WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

// Behind the Caddy reverse proxy (TLS terminator) the backend receives plain HTTP, so it
// must honor X-Forwarded-Proto/-For to know the original request was HTTPS. Without this the
// session cookie's `Secure` flag (set from Request.IsHttps) would be dropped and OAuth/SignalR
// would build wrong absolute URLs. The backend port is never published to the host - only the
// Caddy container on the internal docker network can reach it - so trusting the headers is safe.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    var knownProxy = builder.Configuration["ForwardedHeaders:KnownProxy"];
    if (System.Net.IPAddress.TryParse(knownProxy, out var proxyAddress))
        options.KnownProxies.Add(proxyAddress);
    foreach (var network in builder.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [])
    {
        var parts = network.Split('/', 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 2
            && System.Net.IPAddress.TryParse(parts[0], out var prefix)
            && int.TryParse(parts[1], out var prefixLength))
            options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, prefixLength));
    }
});

// Google sign-in + session JWT (docs/development-guide.md §3). The auth adapters/options are registered
// here and the resolved JwtOptions (with its signing key) is read back so the JWT bearer
// validator and the token issuer share the exact same key.
var jwtOptions = builder.Services.AddAuthInfrastructure(builder.Configuration);

var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (string.IsNullOrWhiteSpace(dataProtectionKeysPath) && builder.Environment.IsProduction())
    dataProtectionKeysPath = "/app/data-protection-keys";

var dataProtection = builder.Services
    .AddDataProtection()
    .SetApplicationName("EnglishAI");

if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

// Encrypting the persisted keys at rest is optional: an absent certificate must never take the
// site down, because the keys are already persisted to a durable volume above (that is what keeps
// auth cookies valid across restarts). Without the certificate the key ring is simply unencrypted,
// which is logged as a warning once the host is built - never a startup exception.
var dataProtectionCertificatePath = builder.Configuration["DataProtection:CertificatePath"];
var dataProtectionCertificatePassword = builder.Configuration["DataProtection:CertificatePassword"];
string? dataProtectionWarning = null;
if (!string.IsNullOrWhiteSpace(dataProtectionCertificatePath) && File.Exists(dataProtectionCertificatePath))
{
    var certificate = new X509Certificate2(
        dataProtectionCertificatePath,
        dataProtectionCertificatePassword,
        X509KeyStorageFlags.EphemeralKeySet);
    dataProtection.ProtectKeysWithCertificate(certificate);
}
else if (builder.Environment.IsProduction())
{
    dataProtectionWarning =
        "Data Protection keys are persisted but NOT encrypted at rest: no PKCS#12 certificate at " +
        $"'{dataProtectionCertificatePath ?? "(DataProtection:CertificatePath unset)"}'. " +
        "Drop a keys.pfx there and set DATA_PROTECTION_CERTIFICATE_PASSWORD to enable encryption.";
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey!));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep the raw "sub" claim type
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };

        // The web session token lives in an HttpOnly cookie, so pull it from there for both
        // HTTP requests and SignalR negotiation. The native mobile shell instead sends the
        // token in an Authorization: Bearer header - when there's no cookie we leave the token
        // unset so the default handler reads that header. So both transports authenticate.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue(jwtOptions.CookieName, out var cookieToken))
                {
                    context.Token = cookieToken;
                }
                else if (context.Request.Path.StartsWithSegments("/hubs")
                         && context.Request.Query.TryGetValue("access_token", out var hubToken))
                {
                    // SignalR WebSocket connections can't set an Authorization header, so the
                    // native client passes the token via the access_token query string instead.
                    context.Token = hubToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// Every endpoint requires an authenticated user by default (the app is Google-only).
// The few public endpoints (health, the Google sign-in/logout) opt out with AllowAnonymous.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Resolves the authenticated learner (JWT sub) for the ownership pipeline behavior, so a
// caller can only act on their own learner data.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();

// ---- Distributed perimeter defence (DoS / brute-force shield) ----
// Redis owns the authoritative fixed-window counters, so adding replicas never multiplies quota.
// It is deliberately safe:
//   * Real-time hubs, the health probe and media (audio/image) streams are EXEMPT - those
//     legitimately burst or stay long-lived, and throttling them would break the app.
//   * Authenticated callers are partitioned by their own user id (one noisy user can't starve
//     others); anonymous callers by forwarded client IP.
//   * The sign-in surface (/api/auth) gets a tighter window since it's the brute-force target.
//   * Redis outages fail closed for sensitive/write traffic; bounded core reads use a small local
//     emergency fallback so the product can still render status/content while Redis recovers.
var rateLimitingEnabled = distributedRateLimitOptions.Enabled;

var hangfireEnabled = builder.Configuration.GetValue("Hangfire:Enabled", false);
if (hangfireEnabled)
{
    builder.Services.AddEnglishAiHangfireStorage(builder.Configuration, requireStorage: false);
    builder.Services.AddScoped<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>();
}

var app = builder.Build();

if (databaseCommand is not null)
{
    var exitCode = await DatabaseCommand.RunAsync(app.Services, databaseCommand, app.Logger, app.Lifetime.ApplicationStopping);
    Environment.ExitCode = exitCode;
    return;
}

if (curriculumCommand is not null)
{
    var exitCode = await CurriculumCommand.RunAsync(app.Services, curriculumCommand, app.Logger, app.Lifetime.ApplicationStopping);
    Environment.ExitCode = exitCode;
    return;
}

if (leaderboardCommand is not null)
{
    var exitCode = await LeaderboardCommand.RunAsync(app.Services, app.Logger, app.Lifetime.ApplicationStopping);
    Environment.ExitCode = exitCode;
    return;
}

if (dataProtectionWarning is not null)
    app.Logger.LogWarning("{DataProtectionWarning}", dataProtectionWarning);

_ = app.Services.GetRequiredService<EnglishAiMetrics>();

// Must run before anything that inspects the scheme/host or client IP (auth cookie, OAuth).
app.UseForwardedHeaders();
app.UseMiddleware<CorrelationIdMiddleware>();

// Request logging is the OUTERMOST app middleware so it observes the FINAL, translated status
// code. The exception handler runs INSIDE it: when a handler throws a known exception (e.g.
// NotFoundException → 404, ValidationException → 400) the handler converts it to the right status
// and does not rethrow, so request logging records that real status. If these were reversed, every
// handled exception would bubble through request logging first and be logged as a scary "responded
// 500" error (with a stack trace) before being turned into its true 4xx - flooding the admin server
// page with false errors. Genuine unhandled exceptions are still logged once, with their stack
// trace, by the exception handler's own LogError.
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (context, _, exception) =>
        context.Response.StatusCode == StatusCodes.Status499ClientClosedRequest
            ? Serilog.Events.LogEventLevel.Information
            : exception is not null || context.Response.StatusCode >= 500
                ? Serilog.Events.LogEventLevel.Error
                : context.Response.StatusCode >= 400
                    ? Serilog.Events.LogEventLevel.Warning
                    : Serilog.Events.LogEventLevel.Information;
});
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<MetricsProtectionMiddleware>();
// Must precede auth so preflight (OPTIONS) requests from the native shell are answered.
app.UseCors(MobileCorsPolicy);

app.UseAuthentication();
app.UseMiddleware<AiRequestContextMiddleware>();

// After authentication so buckets can be keyed per user id (see ResolveClientKey); guarded by
// the same config flag as the registration so the DI options always exist when this runs.
if (rateLimitingEnabled)
{
    app.UseMiddleware<DistributedRateLimitMiddleware>();
}

app.UseAuthorization();
app.UseMiddleware<MandatoryVocabularyReviewMiddleware>();

static Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsJsonAsync(new
    {
        status = report.Status.ToString().ToLowerInvariant(),
        checks = report.Entries.ToDictionary(
            entry => entry.Key,
            entry => new { status = entry.Value.Status.ToString().ToLowerInvariant() })
    });
}

app.MapGet("/health", () => Results.Ok(new { status = "live" })).AllowAnonymous();
app.MapGet("/health/live", () => Results.Ok(new { status = "live" })).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse
}).AllowAnonymous();
app.MapPrometheusScrapingEndpoint("/metrics").AllowAnonymous();
app.MapGet("/api/speaking/live/capabilities", (
    Microsoft.Extensions.Options.IOptions<SpeakingLiveOptions> options) =>
    Results.Ok(new { enabled = options.Value.Enabled }));
app.MapAuthEndpoints();
app.MapPublicMetricsEndpoints();
app.MapDeveloperApiEndpoints();
app.MapPlacementEndpoints();
app.MapSpeakingEndpoints();
app.MapLearningEndpoints();
app.MapVocabularyEndpoints();
app.MapNotificationEndpoints();
app.MapLevelEndpoints();
app.MapVideoEndpoints();
app.MapReadingEndpoints();
app.MapBookEndpoints();
app.MapListeningEndpoints();
app.MapGrammarEndpoints();
app.MapAssistantEndpoints();
app.MapTranslationEndpoints();
app.MapWritingEndpoints();
app.MapGamificationEndpoints();
app.MapSubscriptionEndpoints();
app.MapReferralEndpoints();
app.MapRetentionEndpoints();
app.MapAdminEndpoints();
app.MapImageEndpoints();
app.MapSupportEndpoints();
app.MapHub<NotificationsHub>("/hubs/notifications");
app.MapHub<CompetitionHub>("/hubs/competition");
app.MapHub<SpeakingLiveHub>("/hubs/speaking-live");
app.MapHub<AccentTutorLiveHub>("/hubs/accent-tutor-live").RequireAuthorization();
app.MapHub<SupportHub>("/hubs/support");

app.Run();

// Exposed so integration tests can spin up the app via WebApplicationFactory<Program>.
public partial class Program;
